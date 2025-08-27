using UnityEngine;
using UnityEngine.Pool;

/// A simple projectile weapon that pulls all display/state from its WeaponDefinition,
/// supports upgrades via IUpgradableWeapon, and raises an IconChanged event so the UI
/// can refresh in-place without rebuilding the whole list.
public class GenericProjectileWeapon : Weapon, IUpgradableWeapon
{
    [Header("Definition & Startup")]
    [SerializeField] private WeaponDefinition definitionAsset;
    [SerializeField, Min(1)] private int startLevel = 1;

    [Header("Upgrade Caps")]
    [SerializeField, Min(1)] private int maxLevel = 5;

    [Header("Per-Level Gains")]
    [SerializeField] private float damagePerLevel = 2f;
    [SerializeField] private float projectileSpeedPerLevel = 0.5f;
    [SerializeField] private float rangePerLevel = 0f;
    [SerializeField] private float piercesPerLevel = 0;

    // Current upgrade level (1-based)
    [SerializeField, Min(1)] private int level = 1;

    // Who owns the projectiles (used to mark Damager/source)
    private GameObject ownerRoot;

    // Runtime icon change notification (UI listens for this)
    public event System.Action<Sprite> IconChanged;

    // Projectile pooling
    private IObjectPool<Projectile> pool;
    [SerializeField] private bool collectionCheck = true;
    [SerializeField] private int defaultCapacity = 64;
    [SerializeField] private int maxSize = 256;

    protected override void Awake()
    {
        base.Awake();

        // Owner for damage/team
        ownerRoot = transform.root ? transform.root.gameObject : gameObject;

        // If provided via inspector, push definition + initial level into the base
        if (definitionAsset)
        {
            // If your base Weapon exposes SetDefinition(def, level) use it;
            // otherwise assign Definition and update the icon below.
            SetDefinition(definitionAsset, startLevel);
            level = Mathf.Max(1, startLevel);
        }

        // Ensure initial runtime icon matches level
        UpdateRuntimeIcon();

        // Build pool lazily when we first shoot; initialized parts here are enough
    }

    // Shoot is invoked by your input/attack logic with a normalized direction
    protected override void Shoot(Vector2 dir)
    {
        var def = Definition;
        if (!def || !def.projectilePrefab) return;

        BuildPoolIfNeeded(def);

        var proj = pool.Get();
        proj.transform.position = muzzle ? muzzle.position : transform.position;

        // Configure contact damage (if Damager component present)
        var hb = proj.GetComponent<Damager>();
        if (hb) hb.Configure(ownerRoot, def.targetLayers, def.damage);

        // Initialise projectile logic (your Projectile class should read these)
        proj.Init(ownerRoot, def.targetLayers, def.damage, dir);
        proj.SetRuntime(
            speedOverride: def.projectileSpeed,
            rangeOverride: def.range,
            obstacleOverride: def.obstacleLayers,
            maxPiercesOverride: def.maxPierces,
            pierceObstaclesOverride: def.pierceThroughObstacles,
            radiusOverride: null,
            vfxOverride: null
        );
        proj.ObjectPool = pool;

        // Muzzle flash
        if (def.muzzleFlashPrefab && muzzle)
            VFX.Spawn(def.muzzleFlashPrefab, muzzle.position, transform.rotation, 0.1f);
    }

    private void BuildPoolIfNeeded(WeaponDefinition def)
    {
        if (pool != null || !def || !def.projectilePrefab) return;

        pool = new ObjectPool<Projectile>(
            () =>
            {
                var go = Instantiate(def.projectilePrefab);
                var p = go.GetComponent<Projectile>();
                if (!p) p = go.AddComponent<Projectile>();
                return p;
            },
            p => p.gameObject.SetActive(true),
            p => p.gameObject.SetActive(false),
            p => { if (p) Destroy(p.gameObject); },
            collectionCheck, defaultCapacity, maxSize
        );
    }

    // === Upgrade plumbing =================================================
    public bool TryPreviewUpgrade(int levels, out UpgradeDelta delta, out string reason)
    {
        delta = default; reason = "";

        int target  = Mathf.Min(maxLevel, level + Mathf.Max(0, levels));
        int applied = target - level;
        if (applied <= 0) { reason = "Already max level."; return false; }

        // normal continuous deltas
        delta.damage          = applied * damagePerLevel;
        delta.projectileSpeed = applied * projectileSpeedPerLevel;
        delta.range           = applied * rangePerLevel;

        // ★ fractional pierce accrual → only increments when a whole is earned
        // Using (level-1) so level 1 starts with zero accrued.
        int startWhole = Mathf.FloorToInt((level  - 1) * piercesPerLevel);
        int endWhole   = Mathf.FloorToInt((target - 1) * piercesPerLevel);
        int pierceGain = Mathf.Max(0, endWhole - startWhole);

        delta.pierces = pierceGain;

        return !delta.IsEmpty;
    }

    public bool TryApplyUpgrade(int levels, out int appliedLevels, out string reason)
    {
        appliedLevels = 0;
        if (!TryPreviewUpgrade(levels, out var d, out reason)) return false;

        var def = Definition;
        if (!def) { reason = "No definition."; return false; }

        int target = Mathf.Min(maxLevel, level + Mathf.Max(0, levels));
        appliedLevels = target - level;
        level = target;

        // commit the previewed changes
        def.damage          += d.damage;
        def.projectileSpeed += d.projectileSpeed;
        def.range           += d.range;
        def.maxPierces      += d.pierces;   // ★ only increases when a whole was earned

        UpdateRuntimeIcon();
        return appliedLevels > 0;
    }

    private void UpdateRuntimeIcon()
    {
        Sprite s = Definition ? Definition.GetIconForLevel(level) : null;
        if (s != icon) // 'icon' is provided by the base Weapon (UI reads it)
        {
            icon = s;
            IconChanged?.Invoke(icon);
        }

        // also update the in-world sprite on the prefab instance
        if (spriteRenderer) spriteRenderer.sprite = s;
    }
}
