using UnityEngine;
using UnityEngine.Pool;

public class GenericProjectileWeapon : Weapon, IUpgradableWeapon
{
    [Header("Data")]
    [SerializeField] private WeaponDefinition definitionAsset;
    [SerializeField, Min(1)] private int startLevel = 1;

    [Header("Upgrades")]
    [SerializeField, Min(1)] private int maxLevel = 5;
    [SerializeField] private float damagePerLevel = 2f;
    [SerializeField] private float projectileSpeedPerLevel = 0.5f;
    [SerializeField] private float rangePerLevel = 0f;
    [SerializeField] private int   piercesPerLevel = 0;

    // Track current level locally for upgrades
    [SerializeField, Min(1)] private int level = 1;

    private GameObject ownerRoot;

    private IObjectPool<Projectile> pool;
    [SerializeField] private bool collectionCheck = true;
    [SerializeField] private int defaultCapacity = 64;
    [SerializeField] private int maxSize = 256;

    protected override void Awake()
    {
        base.Awake();
        ownerRoot = transform.root ? transform.root.gameObject : gameObject;

        // Initialise from definition & startLevel as you already do
        if (definitionAsset) SetDefinition(definitionAsset, startLevel);

        // Sync upgrade tracking with startLevel on first run
        if (level < startLevel) level = startLevel;

        BuildPoolIfNeeded();
    }

    // PlayerShooting passes in the aim direction → use it, no input polling here.
    protected override void Shoot(Vector2 dir)
    {
        if (data == null || !data.projectilePrefab) return;

        BuildPoolIfNeeded();

        var p = pool.Get();
        p.transform.position = muzzle ? muzzle.position : transform.position;

        // Configure teams & damage
        var hb = p.GetComponent<Damager>();
        if (hb) hb.Configure(ownerRoot, data.targetLayers, data.damage);

        // Initialise and push runtime settings
        p.Init(ownerRoot, data.targetLayers, data.damage, dir);
        p.SetRuntime(
            speedOverride: data.projectileSpeed,
            rangeOverride: data.range,
            obstacleOverride: data.obstacleLayers,
            maxPiercesOverride: data.maxPierces,
            pierceObstaclesOverride: data.pierceThroughObstacles,
            radiusOverride: null,
            vfxOverride: null
        );
        p.ObjectPool = pool;

        if (data.muzzleFlashPrefab && muzzle)
            VFX.Spawn(data.muzzleFlashPrefab, muzzle.position, transform.rotation, 0.1f);
    }

    private void BuildPoolIfNeeded()
    {
        if (pool != null || data == null || !data.projectilePrefab) return;

        pool = new ObjectPool<Projectile>(
            () =>
            {
                var go = Instantiate(data.projectilePrefab);
                var proj = go.GetComponent<Projectile>();
                if (!proj) proj = go.AddComponent<Projectile>();
                return proj;
            },
            p => p.gameObject.SetActive(true),
            p => p.gameObject.SetActive(false),
            p => { if (p) Destroy(p.gameObject); },
            collectionCheck, defaultCapacity, maxSize
        );
    }

    // ===== IUpgradableWeapon =====

    public bool TryPreviewUpgrade(int levels, out UpgradeDelta delta, out string reason)
    {
        delta = default; reason = "";

        int newLevel = Mathf.Min(maxLevel, level + Mathf.Max(0, levels));
        int applied = newLevel - level;
        if (applied <= 0) { reason = "Already max level."; return false; }

        // Compute per-level gains into a friendly delta
        delta.damage          = applied * damagePerLevel;
        delta.projectileSpeed = applied * projectileSpeedPerLevel;
        delta.range           = applied * rangePerLevel;
        delta.pierces         = applied * piercesPerLevel;

        return !delta.IsEmpty;
    }

    public bool TryApplyUpgrade(int levels, out int appliedLevels, out string reason)
    {
        appliedLevels = 0;
        if (!TryPreviewUpgrade(levels, out var d, out reason)) return false;

        int newLevel = Mathf.Min(maxLevel, level + Mathf.Max(0, levels));
        appliedLevels = newLevel - level;
        level = newLevel;

        // Commit the previewed changes into live runtime data used by Shoot()
        // 'data' is the runtime config you already read in Shoot()
        data.damage          += d.damage;
        data.projectileSpeed += d.projectileSpeed;
        data.range           += d.range;
        data.maxPierces      += d.pierces;

        return appliedLevels > 0;
    }
}