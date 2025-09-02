using UnityEngine;

[CreateAssetMenu(menuName = "Weapons/Weapon Definition", fileName = "NewWeaponDefinition")]
public class WeaponDefinition : ScriptableObject
{
    [Header("Identity")]
    public string Id = System.Guid.NewGuid().ToString("N");
    public string DisplayName = "Unnamed Weapon";

    [Tooltip("Used for upgrade-item category matching (UI drag/drop).")]
    public WeaponCategory category = WeaponCategory.Pistol;

    [Tooltip("UI/rarity tint. Hooked up to RaritySettings.")]
    public Rarity Rarity = Rarity.Common;

    [Header("Prefab")]
    [Tooltip("Prefab that has a Weapon-derived component. Drag the Weapon component from the prefab here.")]
    public Weapon Prefab;

    [Header("Timing")]
    [Tooltip("Base fire cooldown (seconds) used by Weapon.CooldownWindow.")]
    public float baseCooldown = 0.15f;

    [Header("Icons")]
    [Tooltip("Fallback/base icon (also used when LevelIcons slot is empty).")]
    public Sprite Icon;

    [Header("Level Stats (optional)")]
    [SerializeField] private WeaponLevelStats[] levelStats;     // size = number of tiers you want

    [Tooltip("[0] = Level 1, [1] = Level 2, ... Missing/null entries fall back to Icon.")]
    public Sprite[] LevelIcons;

    [Header("Projectile / Damage")]
    [Tooltip("Projectile prefab spawned when this weapon fires.")]
    public GameObject projectilePrefab;

    [Tooltip("Base damage per projectile.")]
    public float damage = 10f;

    [Tooltip("Projectile speed in units/second.")]
    public float projectileSpeed = 12f;

    [Tooltip("How far a projectile travels before despawning.")]
    public float range = 10f;

    [Tooltip("How many targets a projectile can pierce before despawning.")]
    public int maxPierces = 0;

    [Tooltip("If true, the projectile can pierce colliders on obstacleLayers as well as targets.")]
    public bool pierceThroughObstacles = false;

    [Header("Collision Layers")]
    [Tooltip("Layers considered valid targets (will receive damage).")]
    public LayerMask targetLayers;

    [Tooltip("Layers considered blocking/obstacle surfaces for travel/piercing checks.")]
    public LayerMask obstacleLayers;

    [Header("VFX")]
    [Tooltip("Optional muzzle flash prefab spawned at the weapon's muzzle when firing.")]
    public GameObject muzzleFlashPrefab;

    // ---- Convenience ----
    public Sprite GetIconForLevel(int level)
    {
        if (LevelIcons != null && LevelIcons.Length > 0)
        {
            int ix = Mathf.Clamp(level - 1, 0, LevelIcons.Length - 1);
            var s = LevelIcons[ix];
            if (s) return s;
        }
        return Icon;
    }

    // Returns a stat block for a given level (1-based). If you ask for a level
    // beyond what you defined, you get the last defined level.
    public WeaponLevelStats GetStatsForLevel(int level)
    {
        if (!HasLevelStats)
        {
            // fall back to the definition’s base fields as “level 1”
            return new WeaponLevelStats {
                damage                 = damage,
                projectileSpeed        = projectileSpeed,
                range                  = range,
                maxPierces             = maxPierces,
                pierceThroughObstacles = pierceThroughObstacles,
                cooldown               = Mathf.Max(0.01f, baseCooldown),
            };
        }

        int idx = Mathf.Clamp(level - 1, 0, levelStats.Length - 1);
        var s = levelStats[idx];

        // safety: if any level forgot to set cooldown, use baseCooldown
        if (s.cooldown <= 0f) s.cooldown = Mathf.Max(0.01f, baseCooldown);
        return s;
    }

    // Preferred by reflection/property lookups
    public WeaponCategory Category => category;

    public bool HasLevelStats => levelStats != null && levelStats.Length > 0;
    public int  LevelStatsCount => HasLevelStats ? levelStats.Length : 0;
}