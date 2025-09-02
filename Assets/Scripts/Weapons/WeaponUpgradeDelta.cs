using UnityEngine;

[System.Serializable]
public struct WeaponUpgradeDelta
{
    public float? setDamage,            addDamage;
    public float? setProjectileSpeed,   addProjectileSpeed;
    public float? setRange,             addRange;
    public int?   setMaxPierces,        addMaxPierces;
    public bool?  setPierceThroughObstacles;
    public float? setCooldown,          addCooldown;

    public bool IsEmpty =>
        setDamage == null && addDamage == null &&
        setProjectileSpeed == null && addProjectileSpeed == null &&
        setRange == null && addRange == null &&
        setMaxPierces == null && addMaxPierces == null &&
        setPierceThroughObstacles == null &&
        setCooldown == null && addCooldown == null;
}