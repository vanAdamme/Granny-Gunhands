using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class InventoryItemsUI : MonoBehaviour, IInventoryPanel
{
    [Header("Refs")]
    [SerializeField] private ItemInventory itemInventory;          // Player
    [SerializeField] private WeaponInventory weaponInventory;      // Player
    [SerializeField] private Transform gridRoot;                   // has GridLayoutGroup
    [SerializeField] private ItemEntryButton itemButtonPrefab;     // prefab with icon/name/count/use

    [Header("Debug")]
    [SerializeField] private bool verboseLogs = true;

    private readonly List<ItemEntryButton> buttons = new();

    void Awake()
    {
        if (!itemInventory)   itemInventory   = FindFirstObjectByType<ItemInventory>(FindObjectsInactive.Include);
        if (!weaponInventory) weaponInventory = FindFirstObjectByType<WeaponInventory>(FindObjectsInactive.Include);

        if (itemInventory != null) itemInventory.InventoryChanged += Rebuild;
        else if (verboseLogs) Debug.LogWarning("[InventoryItemsUI] No ItemInventory found.");

        if (!gridRoot && verboseLogs) Debug.LogWarning("[InventoryItemsUI] gridRoot is not assigned.");
        if (!itemButtonPrefab && verboseLogs) Debug.LogWarning("[InventoryItemsUI] itemButtonPrefab is not assigned.");
    }

    void OnEnable()
    {
        // If the panel GO stays enabled (recommended), this still runs once at boot.
        Rebuild();
    }

    void OnDestroy()
    {
        if (itemInventory != null) itemInventory.InventoryChanged -= Rebuild;
    }

    public void RefreshPanel() => Rebuild();

    private void Rebuild()
    {
        // Guard rails
        if (!itemInventory || !gridRoot || !itemButtonPrefab)
        {
            if (verboseLogs)
                Debug.LogWarning("[InventoryItemsUI] Missing refs. Inventory, gridRoot, or prefab not set.");
            return;
        }

        // Clear
        for (int i = 0; i < buttons.Count; i++)
            if (buttons[i]) Destroy(buttons[i].gameObject);
        buttons.Clear();

        var list = itemInventory.GetItems();
        if (verboseLogs) Debug.Log($"[InventoryItemsUI] Rebuild {list.Count} stacks");

        for (int i = 0; i < list.Count; i++)
        {
            var stack = list[i];
            if (!stack.def)
            {
                if (verboseLogs) Debug.LogWarning($"[InventoryItemsUI] Null ItemDefinition at index {i}");
                continue;
            }

            var b = Instantiate(itemButtonPrefab, gridRoot);
            b.Bind(stack, UseItem);
            buttons.Add(b);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)gridRoot);
    }

    private void UseItem(ItemDefinition def)
    {
        if (!def || !itemInventory) return;

        // Choose the object that should "use" the item (usually the player that holds WeaponInventory)
        var userGO = weaponInventory ? weaponInventory.gameObject : itemInventory.gameObject;

        if (def is InventoryItemDefinition usable && userGO)
        {
            if (usable.TryUse(userGO))
            {
                itemInventory.Remove(def, 1);
                Rebuild(); // reflect consumption immediately
            }
        }
    }
}