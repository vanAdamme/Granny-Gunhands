using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryItemsUI : MonoBehaviour, IInventoryPanel
{
    [Header("UI")]
    [SerializeField] private Transform gridRoot;               // Should have a GridLayoutGroup
    [SerializeField] private ItemEntryButton itemButtonPrefab; // Icon + name + count + Use button
    [SerializeField] private bool verboseLogs = true;

    // Runtime bindings (resolved automatically)
    private IPlayerContext ctx;
    private ItemInventory itemInventory;
    private WeaponInventory weaponInventory;

    private readonly List<ItemEntryButton> buttons = new();

    void OnEnable()
    {
        // Bind in case we were disabled at boot
        StartCoroutine(BindWhenAvailable());
    }

    void OnDisable()
    {
        // Safety: unhook if we were subscribed
        if (itemInventory != null) itemInventory.InventoryChanged -= Rebuild;
    }

    public void RefreshPanel() => Rebuild();

    private IEnumerator BindWhenAvailable()
    {
        // Keep trying until the player prefab instance exists and exposes ItemInventory
        while (itemInventory == null)
        {
            TryResolveContextAndInventories();
            if (itemInventory == null) yield return null; // wait a frame and try again
        }

        // Subscribe once we have the live instance
        itemInventory.InventoryChanged += Rebuild;

        // Initial paint
        Rebuild();
    }

    private void TryResolveContextAndInventories()
    {
        if (ctx == null)
        {
            // Prefer a known player controller that implements IPlayerContext
            var player = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);
            ctx = player as IPlayerContext;

            // Fallback sweep: any behaviour that implements IPlayerContext
            if (ctx == null)
            {
                var any = FindFirstObjectByType<MonoBehaviour>(FindObjectsInactive.Include);
                if (any is IPlayerContext asCtx) ctx = asCtx;
            }
        }

        if (ctx != null && itemInventory == null)
        {
            // Crucial: use the SAME ItemInventory the pickup adds to (owned by the player prefab)
            itemInventory = (ctx as Component)?.GetComponentInChildren<ItemInventory>();
            if (verboseLogs && itemInventory != null)
                Debug.Log($"[InventoryItemsUI] Bound ItemInventory (instance {itemInventory.GetInstanceID()})");
        }

        if (weaponInventory == null && ctx is Component c)
        {
            weaponInventory = c.GetComponentInChildren<WeaponInventory>();
        }

        if (!gridRoot && verboseLogs) Debug.LogWarning("[InventoryItemsUI] gridRoot is not assigned.");
        if (!itemButtonPrefab && verboseLogs) Debug.LogWarning("[InventoryItemsUI] itemButtonPrefab is not assigned.");
    }

    private void Rebuild()
    {
        // Guard rails
        if (!itemInventory || !gridRoot || !itemButtonPrefab)
        {
            if (verboseLogs)
                Debug.LogWarning("[InventoryItemsUI] Missing refs. Ensure player has ItemInventory and the UI has gridRoot & prefab.");
            return;
        }

        // Clear existing UI
        for (int i = 0; i < buttons.Count; i++)
            if (buttons[i]) Destroy(buttons[i].gameObject);
        buttons.Clear();

        IReadOnlyList<ItemInventory.Stack> list = itemInventory.GetItems();
        if (verboseLogs) Debug.Log($"[InventoryItemsUI] Rebuild {list.Count} stacks");

        // Build rows
        for (int i = 0; i < list.Count; i++)
        {
            var stack = list[i];
            if (!stack.def)
            {
                if (verboseLogs) Debug.LogWarning($"[InventoryItemsUI] Null ItemDefinition at index {i}");
                continue;
            }

            var row = Instantiate(itemButtonPrefab, gridRoot);
            row.Bind(stack, UseItem);
            buttons.Add(row);
        }

        // Force layout pass (helps when panel opens while paused)
        if (gridRoot is RectTransform rt)
            LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
    }

    private void UseItem(ItemDefinition def)
    {
        if (!def || !itemInventory) return;

        // The "user" for TryUse should be the player (weapon owner) if present
        GameObject userGO = weaponInventory ? weaponInventory.gameObject : (ctx as Component)?.gameObject;
        if (!userGO) userGO = itemInventory.gameObject;

        if (def is InventoryItemDefinition usable)
        {
            if (usable.TryUse(userGO))
            {
                itemInventory.Remove(def, 1); // consume one
                Rebuild();                    // reflect consumption immediately
            }
        }
    }
}