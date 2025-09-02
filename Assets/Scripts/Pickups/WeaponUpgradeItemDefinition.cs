using UnityEngine;

[CreateAssetMenu(menuName = "Items/Weapon Upgrade (Manual)")]
public class WeaponUpgradeItemDefinition : InventoryItemDefinition
{
    [Header("Targeting")]
    public WeaponCategory category = WeaponCategory.Pistol;

    [Header("Manual Stat Changes")]
    public WeaponUpgradeDelta delta;

    public override bool CanUse(GameObject user) => false;  // drag-only
    public override bool TryUse(GameObject user) => false;  // drag-only

    // Called by your drag/drop UI: try to apply this item to a weapon.
    // We keep 'appliedLevels' so existing UI can show "+1" on success.
    public bool TryApplyTo(Weapon weapon, out int appliedLevels, out string reason)
    {
        appliedLevels = 0;
        if (!weapon) { reason = "No weapon."; return false; }

        // Category gate
        var def = weapon.Definition;
        if (!def) { reason = "Weapon has no definition."; return false; }
        if (def.Category != category)
        {
            reason = $"Requires {category} weapon.";
            return false;
        }

        if (delta.IsEmpty)
        {
            reason = "Upgrade has no stat changes.";
            return false;
        }

        reason = "Weapon does not support manual upgrades.";
        return false;
    }
}