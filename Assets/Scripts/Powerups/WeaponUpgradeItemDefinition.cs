using UnityEngine;

[CreateAssetMenu(menuName = "Items/Weapon Upgrade")]
public class WeaponUpgradeItemDefinition : InventoryItemDefinition
{
    [Header("Upgrade Rule")]
    public WeaponCategory category = WeaponCategory.Pistol;
    [Min(1)] public int levels = 1;
    [Tooltip("Only consider currently equipped weapons of this category.")]
    public bool equippedOnly = true; // (kept for future expansion)

    public override bool TryUse(GameObject user)
    {
        if (!user) return false;

        var inv = user.GetComponentInChildren<WeaponInventory>();
        if (!inv) return false;

        // Use the overload that reports what actually upgraded
        if (inv.UpgradeLowestEquippedOf(category, levels, out var upgraded, out var applied) && applied > 0)
        {
            WeaponUpgradePickup.RaiseUpgraded(upgraded, applied); // fires your toast listener
            return true; // consume one item
        }
        return false;
    }
}