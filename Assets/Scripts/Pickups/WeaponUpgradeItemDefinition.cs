using System.Reflection;
using UnityEngine;

[CreateAssetMenu(menuName = "Items/Weapon Upgrade")]
public class WeaponUpgradeItemDefinition : InventoryItemDefinition
{
    [Header("Targeting")]
    public WeaponCategory category = WeaponCategory.Pistol;

    [Header("Manual Stat Changes (optional)")]
    public WeaponUpgradeDelta delta;           // will be used when your weapon implements IUpgradableWeaponV2

    [Header("Legacy Levels (fallback)")]
    [Min(1)] public int levels = 1;

    // Drag-only item in your UI; do not consume via "Use"
    // NOTE: no 'override' — your base may not define these
    public override bool CanUse(GameObject user) => false;
    public override bool TryUse(GameObject user) => false;

    // ===== Preview helpers used by drag/drop UI =====

    public bool TryPreviewFor(Weapon weapon, out UpgradeDelta legacyDelta, out string reason)
    {
        legacyDelta = default;
        if (!IsCategoryMatch(weapon, out reason)) return false;

        // If you still have legacy per-level upgrades on the weapon, ask it to preview.
        if (weapon is IUpgradableWeapon upgradable)
        {
            return upgradable.TryPreviewUpgrade(levels, out legacyDelta, out reason);
        }

        reason = "Weapon does not support legacy level upgrades.";
        return false;
    }

    public bool TryApplyTo(Weapon weapon, out int appliedLevels, out string reason)
    {
        appliedLevels = 0;
        if (!IsCategoryMatch(weapon, out reason)) return false;

        // Prefer the new manual-delta path if the weapon supports it.
        if (weapon is IUpgradableWeaponV2 v2)
        {
            if (delta.IsEmpty) { reason = "No stat changes defined."; return false; }
            bool ok = v2.TryApplyUpgrade(delta, out reason);
            return ok;
        }

        // Fallback to legacy level-based upgrades
        if (weapon is IUpgradableWeapon up)
            return up.TryApplyUpgrade(levels, out appliedLevels, out reason);

        reason = "Weapon does not support upgrades.";
        return false;
    }

    // --- category check (same logic you had before) ---
    private bool IsCategoryMatch(Weapon weapon, out string reason)
    {
        reason = "";
        if (!weapon) { reason = "No weapon."; return false; }

        if (!TryGetWeaponCategory(weapon, out var weaponCat))
        {
            reason = "Weapon has no category.";
            return false;
        }
        if (weaponCat != category)
        {
            reason = $"Requires {category} weapon.";
            return false;
        }
        return true;
    }

    private static bool TryGetWeaponCategory(Weapon weapon, out WeaponCategory cat)
    {
        cat = default;
        var def = weapon?.Definition;
        if (def == null) return false;

        var t = def.GetType();

        // Prefer property 'Category'
        var p = t.GetProperty("Category", BindingFlags.Instance | BindingFlags.Public);
        if (p != null && p.PropertyType == typeof(WeaponCategory))
        {
            cat = (WeaponCategory)p.GetValue(def, null);
            return true;
        }

        // Fallback field 'category'
        var f = t.GetField("category", BindingFlags.Instance | BindingFlags.Public);
        if (f != null && f.FieldType == typeof(WeaponCategory))
        {
            cat = (WeaponCategory)f.GetValue(def);
            return true;
        }

        return false;
    }
}