public interface IUpgradableWeapon
{
    /// Non-destructive preview. Return true if an upgrade would change anything.
    bool TryPreviewUpgrade(int levels, out UpgradeDelta delta, out string reason);

    /// Apply the upgrade. Return true if anything changed.
    bool TryApplyUpgrade(int levels, out int appliedLevels, out string reason);
}