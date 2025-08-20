using UnityEngine;
using UnityEngine.Pool;

public class GenericProjectileWeapon : Weapon
{
    [Header("Data")]
    [SerializeField] private WeaponDefinition definitionAsset;
    [SerializeField, Min(1)] private int startLevel = 1;

    private GameObject ownerRoot;

    private IObjectPool<Projectile> pool;
    [SerializeField] private bool collectionCheck = true;
    [SerializeField] private int defaultCapacity = 64;
    [SerializeField] private int maxSize = 256;

    protected override void Awake()
    {
        base.Awake();
        ownerRoot = transform.root ? transform.root.gameObject : gameObject;

        if (definitionAsset) SetDefinition(definitionAsset, startLevel);
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
}