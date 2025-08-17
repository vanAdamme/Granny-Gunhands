using System;
using System.Collections.Generic;
using UnityEngine;

public enum Hand { Left, Right }

public sealed class WeaponInventory : MonoBehaviour
{
    [Header("Inventory (instances only at runtime)")]
    [SerializeField] private List<Weapon> inventory = new();

    [Header("Mounts (optional)")]
    [SerializeField] private Transform leftMount;
    [SerializeField] private Transform rightMount;

    [Header("Rules")]
    [SerializeField] private bool allowSameInBothHands = false;

    private int leftIndex  = -1;
    private int rightIndex = -1;

    public Weapon Left  => IsValid(leftIndex)  ? inventory[leftIndex]  : null;
    public Weapon Right => IsValid(rightIndex) ? inventory[rightIndex] : null;

    public event System.Action InventoryChanged;
    private void RaiseInventoryChanged() => InventoryChanged?.Invoke();

    public event Action<Hand, Weapon> OnEquippedChanged;

    void Awake()
    {
        // Ensure anything dragged in is a scene instance (instantiate if not)
        for (int i = 0; i < inventory.Count; i++)
        {
            var w = inventory[i];
            if (!w) continue;

            if (!w.gameObject.scene.IsValid())
            {
                // Someone dragged a prefab asset in—replace with a live instance
                w = Instantiate(w, transform);
                inventory[i] = w;
            }

            w.gameObject.SetActive(false);
        }

        // Optional defaults
        if (inventory.Count > 0)
        {
            Equip(Hand.Left,  0, true, false);
            Equip(Hand.Right, Mathf.Min(1, inventory.Count - 1), true, false);
            OnEquippedChanged?.Invoke(Hand.Left,  Left);
            OnEquippedChanged?.Invoke(Hand.Right, Right);
        }
    }

    // ---------- Public API ----------

    public IReadOnlyList<Weapon> GetInventory() => inventory;
    public Weapon GetWeapon(Hand hand) => hand == Hand.Left ? Left : Right;

    /// <summary>Add an existing scene instance to the inventory (will be deactivated until equipped).</summary>
    public Weapon AddWeaponInstance(Weapon instance, bool autoEquipToEmptyHand = true)
    {
        if (!instance) return null;

        // Normalise parent
        instance.transform.SetParent(transform, false);
        instance.gameObject.SetActive(false);

        inventory.Add(instance);
        RaiseInventoryChanged();

        if (autoEquipToEmptyHand)
            AutoEquipIfEmpty(inventory.Count - 1);

        return instance;
    }

    /// <summary>Instantiate from a prefab (asset or instance) and add.</summary>
    public Weapon AddWeapon(Weapon prefab, bool autoEquipToEmptyHand = true)
    {
        if (!prefab) return null;
        // Always instantiate to ensure a scene instance
        var instance = Instantiate(prefab, transform);
        instance.gameObject.SetActive(false);
        inventory.Add(instance);
        RaiseInventoryChanged();

        if (autoEquipToEmptyHand)
            AutoEquipIfEmpty(inventory.Count - 1);

        return instance;
    }

    public Weapon AddWeapon(WeaponDefinition def, bool autoEquipToEmptyHand = true, bool allowDuplicates = false)
    {
        if (!def) return null;
        if (!allowDuplicates && HasWeapon(def.Id))
        {
            // TODO: UIController.Instance?.ShowToast($"{def.DisplayName} already owned");
            return null;
        }
        var instance = WeaponFactory.Create(def, transform);
        if (!instance) return null;

        inventory.Add(instance);
        RaiseInventoryChanged();
        if (autoEquipToEmptyHand) AutoEquipIfEmpty(inventory.Count - 1);
        return instance;
    }

    /// <summary>Remove and destroy an instance.</summary>
    public bool RemoveWeapon(Weapon instance)
    {
        if (!instance) return false;

        // Unequip if equipped
        if (Left  == instance)  Unequip(Hand.Left);
        if (Right == instance)  Unequip(Hand.Right);

        bool removed = inventory.Remove(instance);
        if (removed)
        {
            Destroy(instance.gameObject);
            RaiseInventoryChanged();
        }
        return removed;
    }

    public void Cycle(Hand hand, int direction = 1)
    {
        if (inventory.Count == 0) return;

        int cur = (hand == Hand.Left) ? leftIndex : rightIndex;
        if (cur < 0) cur = 0;

        for (int step = 0; step < inventory.Count; step++)
        {
            cur = Mod(cur + direction, inventory.Count);
            if (!IsValid(cur)) continue;

            if (!allowSameInBothHands)
            {
                if (hand == Hand.Left  && cur == rightIndex) continue;
                if (hand == Hand.Right && cur == leftIndex)  continue;
            }

            Equip(hand, cur, true, true);
            return;
        }
    }

    public void Equip(Hand hand, int index, bool applyMount, bool raiseEvents)
    {
        if (!IsValid(index)) return;

        // Deactivate previous
        var prev = (hand == Hand.Left) ? Left : Right;
        if (prev) prev.gameObject.SetActive(false);

        if (hand == Hand.Left) leftIndex = index; else rightIndex = index;

        var now = (hand == Hand.Left) ? Left : Right;
        if (now)
        {
            Transform mount = hand == Hand.Left ? leftMount : rightMount;
            if (applyMount)
            {
                now.transform.SetParent(mount ? mount : transform, false);
                now.transform.localPosition = Vector3.zero;
                now.transform.localRotation = Quaternion.identity;
            }
            now.gameObject.SetActive(true);
        }

        if (raiseEvents) OnEquippedChanged?.Invoke(hand, now);
    }

    public void Unequip(Hand hand)
    {
        var w = (hand == Hand.Left) ? Left : Right;
        if (w) w.gameObject.SetActive(false);

        if (hand == Hand.Left) leftIndex = -1; else rightIndex = -1;
        OnEquippedChanged?.Invoke(hand, null);
    }

    // ---------- Helpers ----------

    private void AutoEquipIfEmpty(int newIndex)
    {
        if (Left == null)  { Equip(Hand.Left,  newIndex, true, true); return; }
        if (Right == null) { Equip(Hand.Right, newIndex, true, true); return; }
        // otherwise leave it in the backpack list only
    }

    public bool HasWeapon(string defId)
    {
        foreach (var w in GetInventory())
            if (w && w.Definition && w.Definition.Id == defId) return true;
        return false;
    }

    private bool IsValid(int i) => i >= 0 && i < inventory.Count && inventory[i] != null;
    private static int Mod(int a, int n) { int r = a % n; return r < 0 ? r + n : r; }
}