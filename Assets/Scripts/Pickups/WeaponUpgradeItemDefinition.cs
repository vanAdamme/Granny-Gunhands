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

    public bool TryPreviewFor(Weapon weapon, out WeaponUpgradeDelta previewDelta, out string reason)
    {
        previewDelta = default;
        if (!weapon) { reason = "No weapon."; return false; }
        if (!weapon.Definition) { reason = "Weapon has no definition."; return false; }
        if (weapon.Definition.Category != category) { reason = $"Requires {category} weapon."; return false; }
        if (delta.IsEmpty) { reason = "No stat changes."; return false; }
        previewDelta = delta; reason = ""; return true;
    }
    
    public bool TryApplyTo(Weapon weapon, out int appliedLevels, out string reason)
    {
        appliedLevels = 0;
        if (!weapon) { reason = "No weapon."; return false; }
        if (!weapon.Definition) { reason = "Weapon has no definition."; return false; }
        if (weapon.Definition.Category != category) { reason = $"Requires {category} weapon."; return false; }
        if (delta.IsEmpty) { reason = "Upgrade has no stat changes."; return false; }

        if (weapon is IUpgradableWeapon up)
        {
            var ok = up.TryApplyUpgrade(delta, out reason);
            if (ok) { appliedLevels = 1; UpgradeEvents.RaiseApplied(weapon, 1); }
            return ok;
        }
        reason = "Weapon does not support manual upgrades."; return false;
    }
}