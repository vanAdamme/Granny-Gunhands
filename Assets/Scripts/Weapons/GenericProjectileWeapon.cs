using UnityEngine;

public class GenericProjectileWeapon : Weapon, IUpgradableWeapon
{
    [Header("Definition & Startup")]
    [SerializeField] private WeaponDefinition definitionAsset;
    [SerializeField, Min(1)] private int startLevel = 1; // keep if your icon per-level matters

    [Header("Pooling")]
    [SerializeField] private UnityPoolService poolService;

    private GameObject ownerRoot;
    [SerializeField] private WeaponRuntimeStats stats;   // runtime copy
    public WeaponRuntimeStats CurrentStats => stats;

    public event System.Action<Sprite> IconChanged;

    protected override void Awake()
    {
        base.Awake();
        if (!poolService) poolService = FindFirstObjectByType<UnityPoolService>();
        ownerRoot = transform.root ? transform.root.gameObject : gameObject;

        if (definitionAsset) SetDefinition(definitionAsset, startLevel);
        UpdateRuntimeIcon();
    }

    public override void SetDefinition(WeaponDefinition def, int level)
    {
        base.SetDefinition(def, level);  // sets Definition, currentLevel, icon, base cooldown
        ApplyLevelStats();               // pull level-appropriate stats
    }

    protected override void Shoot(Vector2 dir)
    {
        var def = Definition;
        if (!def || !def.projectilePrefab) return;

        Vector3 pos = muzzle ? muzzle.position : transform.position;
        GameObject go = poolService
            ? poolService.Spawn(def.projectilePrefab, pos, Quaternion.identity)
            : Instantiate(def.projectilePrefab, pos, Quaternion.identity);

        go.transform.right = dir;
        var proj = go.GetComponent<Projectile>() ?? go.AddComponent<Projectile>();

        // Use runtime stats (not the SO) for firing
        proj.Init(ownerRoot, def.targetLayers, stats.damage, dir);
        proj.SetRuntime(
            speedOverride: stats.projectileSpeed,
            rangeOverride: stats.range,
            obstacleOverride: def.obstacleLayers,
            maxPiercesOverride: stats.maxPierces,
            pierceObstaclesOverride: stats.pierceThroughObstacles,
            radiusOverride: null,
            vfxOverride: null
        );

        if (def.muzzleFlashPrefab && muzzle)
        {
            if (poolService) poolService.Spawn(def.muzzleFlashPrefab, muzzle.position, muzzle.rotation);
            else Destroy(Instantiate(def.muzzleFlashPrefab, muzzle.position, muzzle.rotation), 0.15f);
        }
    }

    private int ClampLevel(int desired)
    {
        // Cap by number of level icons if present, else allow any 1..N
        int min = 1;
        if (Definition && Definition.LevelIcons != null && Definition.LevelIcons.Length > 0)
            return Mathf.Clamp(desired, min, Definition.LevelIcons.Length);
        return Mathf.Max(min, desired);
    }

    private void RefreshIcon()
    {
        Sprite s = Definition ? Definition.GetIconForLevel(Level) : null;
        icon = s;
        if (spriteRenderer) spriteRenderer.sprite = s;
    }

    // ---- IUpgradableWeapon (level bump) ----
    public bool TryPreviewUpgrade(int levels, out UpgradeDelta delta, out string reason)
    {
        delta = default;     // no auto stat deltas; level decides stats
        reason = "";
        return levels > 0;
    }

    public bool TryApplyUpgrade(int levels, out int appliedLevels, out string reason)
    {
        appliedLevels = 0;
        if (!TryPreviewUpgrade(levels, out _, out reason)) return false;

        currentLevel += levels;          // bump level (uncapped by default)
        appliedLevels = levels;

        // update stats & visuals for the new level
        ApplyLevelStats();

        var sprite = Definition ? Definition.GetIconForLevel(Level) : null;
        icon = sprite;
        if (spriteRenderer) spriteRenderer.sprite = sprite;

        return true;
    }

    private void ApplyLevelStats()
    {
        if (!Definition) return;
        var s = Definition.GetStatsForLevel(Level);

        stats.damage                 = s.damage;
        stats.projectileSpeed        = s.projectileSpeed;
        stats.range                  = s.range;
        stats.maxPierces             = s.maxPierces;
        stats.pierceThroughObstacles = s.pierceThroughObstacles;
        stats.cooldown               = s.cooldown;

        CooldownWindow = stats.cooldown;
    }

    private void UpdateRuntimeIcon()
    {
        Sprite s = Definition ? Definition.GetIconForLevel(Level) : null;
        if (s != icon)
        {
            icon = s;
            IconChanged?.Invoke(icon);
        }
        if (spriteRenderer) spriteRenderer.sprite = s;
    }
}