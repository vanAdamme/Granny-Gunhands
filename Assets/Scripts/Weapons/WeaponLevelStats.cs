using UnityEngine;

[System.Serializable]
public struct WeaponLevelStats
{
    public float damage;
    public float projectileSpeed;
    public float range;
    public int   maxPierces;
    public bool  pierceThroughObstacles;
    [Min(0.01f)] public float cooldown;   // seconds between shots
}