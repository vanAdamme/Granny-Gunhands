using UnityEngine;

[System.Serializable]
public struct WeaponRuntimeStats
{
    public float damage;
    public float projectileSpeed;
    public float range;
    public int   maxPierces;
    public bool  pierceThroughObstacles;
    public float cooldown;              // seconds between shots
}

// Explicit, manual upgrade operations (either Add or Set per stat)
[System.Serializable]
public struct WeaponUpgradeDelta
{
    public float? setDamage, addDamage;
    public float? setProjectileSpeed, addProjectileSpeed;
    public float? setRange, addRange;
    public int?   setMaxPierces, addMaxPierces;
    public bool?  setPierceThroughObstacles;
    public float? setCooldown, addCooldown;

    public bool IsEmpty =>
        setDamage == null && addDamage == null &&
        setProjectileSpeed == null && addProjectileSpeed == null &&
        setRange == null && addRange == null &&
        setMaxPierces == null && addMaxPierces == null &&
        setPierceThroughObstacles == null &&
        setCooldown == null && addCooldown == null;
}

public interface IUpgradableWeaponV2
{
    // Returns true if anything changed
    bool TryApplyUpgrade(WeaponUpgradeDelta delta, out string reason);
    WeaponRuntimeStats CurrentStats { get; }
}