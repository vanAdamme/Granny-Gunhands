using System;

public static class UpgradeEvents
{
    // Fired when an upgrade actually changes a weapon (not when the pickup is collected)
    public static event Action<Weapon, int> OnApplied;

    public static void RaiseApplied(Weapon weapon, int appliedLevels)
        => OnApplied?.Invoke(weapon, appliedLevels);
}