public interface IUpgradableWeapon
{
    /// Apply an explicit stat delta to the weapon's runtime stats.
    /// Return true if anything actually changed; 'reason' explains failures.
    bool TryApplyUpgrade(WeaponUpgradeDelta delta, out string reason);

    WeaponRuntimeStats CurrentStats { get; }
}