using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryItemsUI : MonoBehaviour, IInventoryPanel
{
    [Header("UI")]
    [SerializeField] private Transform gridRoot;               // GridLayoutGroup parent
    [SerializeField] private ItemEntryButton itemButtonPrefab; // item row
    [SerializeField] private TMP_Text sectionHeaderPrefab;     // optional
    [SerializeField] private bool verboseLogs = true;

    // Runtime bindings
    private IPlayerContext ctx;
    private ItemInventory itemInventory;
    private WeaponInventory weaponInventory;

    private readonly List<Object> spawned = new();

    void OnEnable()
    {
        StartCoroutine(BindWhenAvailable());
    }

    void OnDisable()
    {
        if (itemInventory != null) itemInventory.InventoryChanged -= Rebuild;
    }

    public void RefreshPanel() => Rebuild();

    private IEnumerator BindWhenAvailable()
    {
        while (itemInventory == null)
        {
            TryResolveContextAndInventories();
            if (itemInventory == null) yield return null;
        }
        itemInventory.InventoryChanged += Rebuild;
        Rebuild();
    }

    private void TryResolveContextAndInventories()
    {
        // 1) Find a player context (prefer PlayerController)
        if (ctx == null)
        {
            ctx = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include) as IPlayerContext;
            if (ctx == null)
            {
                // Fallback: scan for any MonoBehaviour that implements IPlayerContext
                var all = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                for (int i = 0; i < all.Length && ctx == null; i++)
                    if (all[i] is IPlayerContext asCtx) ctx = asCtx;
            }
        }

        // 2) Bind the SAME ItemInventory the player exposes
        if (ctx != null && itemInventory == null)
        {
            itemInventory = ctx.ItemInventory 
                            ?? (ctx as Component)?.GetComponentInChildren<ItemInventory>(); // last resort
            if (verboseLogs && itemInventory != null)
                Debug.Log($"[InventoryItemsUI] Bound ItemInventory (instance {itemInventory.GetInstanceID()})");
        }

        // 3) Cache weapon inventory for CanUse/TryUse userGO
        if (weaponInventory == null && ctx is Component c)
            weaponInventory = c.GetComponentInChildren<WeaponInventory>();

        if (!gridRoot && verboseLogs) Debug.LogWarning("[InventoryItemsUI] gridRoot not set.");
        if (!itemButtonPrefab && verboseLogs) Debug.LogWarning("[InventoryItemsUI] itemButtonPrefab not set.");
    }

    private void Clear()
    {
        for (int i = 0; i < spawned.Count; i++)
        {
            var o = spawned[i] as Component;
            if (o) Destroy(o.gameObject);
        }
        spawned.Clear();
    }

    private void AddHeader(string title)
    {
        if (!sectionHeaderPrefab) return;
        var h = Instantiate(sectionHeaderPrefab, gridRoot);
        h.text = title;
        spawned.Add(h);
    }

    private void Rebuild()
    {
        if (!itemInventory || !gridRoot || !itemButtonPrefab) return;
        Clear();

        var list = itemInventory.GetItems();
        if (verboseLogs) Debug.Log($"[InventoryItemsUI] Rebuild {list.Count} stacks");

        // Choose the "user" GO for button gating (only for truly usable items)
        GameObject userGO = weaponInventory ? weaponInventory.gameObject : (ctx as Component)?.gameObject;
        if (!userGO) userGO = itemInventory.gameObject;

        // Partition: upgrades (drag onto weapon) vs direct-use items
        var upgrades = new List<ItemInventory.Stack>();
        var useables = new List<ItemInventory.Stack>();
        var others = new List<ItemInventory.Stack>();

        foreach (var s in list)
        {
            if (!s.def) continue;
            if (s.def is WeaponUpgradeItemDefinition) upgrades.Add(s);
            else if (s.def is InventoryItemDefinition) useables.Add(s);
            else others.Add(s);
        }

        upgrades = upgrades.OrderBy(s => s.def.DisplayName).ToList();
        useables = useables.OrderBy(s => s.def.DisplayName).ToList();
        others = others.OrderBy(s => s.def.DisplayName).ToList();

        if (upgrades.Count > 0) AddHeader("Apply to a Weapon (drag onto a weapon)");
        foreach (var s in upgrades)
        {
            // Disable Use button for upgrades; drag is the interaction
            var row = Instantiate(itemButtonPrefab, gridRoot);
            row.Bind(s, itemInventory, /*canUse*/ false, UseItemNoop);
            spawned.Add(row);
        }

        if (useables.Count > 0) AddHeader("Useables");
        foreach (var s in useables)
        {
            var usable = (InventoryItemDefinition)s.def;
            bool canUse = usable.CanUse(userGO);
            var row = Instantiate(itemButtonPrefab, gridRoot);
            row.Bind(s, itemInventory, canUse, UseItem);
            spawned.Add(row);
        }

        if (others.Count > 0) AddHeader("Other");
        foreach (var s in others)
        {
            var row = Instantiate(itemButtonPrefab, gridRoot);
            row.Bind(s, itemInventory, /*canUse*/ false, UseItemNoop);
            spawned.Add(row);
        }

    if (gridRoot is RectTransform rt)
    {
        var auto = rt.GetComponent<GridAutoHeight>();
        if (auto) auto.Recalc();

        LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        var parent = rt.parent as RectTransform;   // Content
        if (parent) LayoutRebuilder.ForceRebuildLayoutImmediate(parent);
        Canvas.ForceUpdateCanvases();
    }
    }

    private void UseItemNoop(ItemDefinition _) { /* intentional: upgrades are drag-only */ }

    private void UseItem(ItemDefinition def)
    {
        if (!def || !itemInventory) return;

        GameObject userGO = weaponInventory ? weaponInventory.gameObject : (ctx as Component)?.gameObject;
        if (!userGO) userGO = itemInventory.gameObject;

        if (def is InventoryItemDefinition usable && usable.TryUse(userGO))
        {
            itemInventory.Remove(def, 1);
            Rebuild();
        }
    }
}