using System.Reflection;
using UnityEngine;

[CreateAssetMenu(menuName = "Items/Weapon Upgrade")]
public class WeaponUpgradeItemDefinition : InventoryItemDefinition
{
    [Header("Upgrade Rule")]
    public WeaponCategory category = WeaponCategory.Pistol;
    [Min(1)] public int levels = 1;

    // Drag-only item
    public override bool CanUse(GameObject user) => false;
    public override bool TryUse(GameObject user) => false;

    // === Preview ==========================================================
    public bool TryPreviewFor(Weapon weapon, out UpgradeDelta delta, out string reason)
    {
        delta = default; reason = "";
        if (!IsCategoryMatch(weapon, out reason)) return false;

        // Preferred path: weapon implements capability
        if (weapon is IUpgradableWeapon up)
            return up.TryPreviewUpgrade(levels, out delta, out reason);

        // No interface? Try a reflection hint: Weapon has method "PreviewUpgrade(int, out UpgradeDelta)"
        var m = weapon.GetType().GetMethod(
            "PreviewUpgrade",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            new[] { typeof(int), typeof(UpgradeDelta).MakeByRefType() },
            null);
        if (m != null)
        {
            object[] args = { levels, null };
            bool ok = (bool)m.Invoke(weapon, args);
            if (ok && args[1] is UpgradeDelta d) { delta = d; return true; }
            reason = reason == "" ? "No preview available." : reason;
            return false;
        }

        // Fallback: we can at least say "will upgrade", but no numbers
        reason = "No preview available.";
        return false;
    }

    // === Apply ============================================================
    public bool TryApplyTo(Weapon weapon, out int appliedLevels, out string reason)
    {
        appliedLevels = 0; reason = "";
        if (!IsCategoryMatch(weapon, out reason)) return false;

        if (weapon is IUpgradableWeapon up)
        {
            var ok = up.TryApplyUpgrade(levels, out appliedLevels, out reason);
            if (ok && appliedLevels > 0) UpgradeEvents.RaiseApplied(weapon, appliedLevels);
            return ok;
        }

        // Fallback: TryUpgrade(int, out int)
        var mTry = weapon.GetType().GetMethod(
            "TryUpgrade",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic,
            null,
            new[] { typeof(int), typeof(int).MakeByRefType() },
            null);

        if (mTry != null)
        {
            object[] args = { levels, 0 };
            bool ok = (bool)mTry.Invoke(weapon, args);
            appliedLevels = (int)args[1];
            if (ok && appliedLevels > 0) UpgradeEvents.RaiseApplied(weapon, appliedLevels);
            if (!ok || appliedLevels <= 0) reason = "No upgrade applied.";
            return ok && appliedLevels > 0;
        }

        // Fallback: ApplyUpgrade(int)
        var mApply = weapon.GetType().GetMethod(
            "ApplyUpgrade",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic,
            null,
            new[] { typeof(int) },
            null);

        if (mApply != null)
        {
            mApply.Invoke(weapon, new object[] { levels });
            appliedLevels = levels;
            UpgradeEvents.RaiseApplied(weapon, appliedLevels);
            return true;
        }

        reason = "Weapon doesn’t support upgrades.";
        return false;
    }

    // === Helpers ==========================================================
    private bool IsCategoryMatch(Weapon weapon, out string reason)
    {
        reason = "";
        if (!weapon || !weapon.Definition) { reason = "No weapon."; return false; }

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
        var p = t.GetProperty("Category", BindingFlags.Instance | BindingFlags.Public);
        if (p != null && p.PropertyType == typeof(WeaponCategory))
        {
            cat = (WeaponCategory)p.GetValue(def, null);
            return true;
        }
        var f = t.GetField("category", BindingFlags.Instance | BindingFlags.Public);
        if (f != null && f.FieldType == typeof(WeaponCategory))
        {
            cat = (WeaponCategory)f.GetValue(def);
            return true;
        }
        return false;
    }
}