using UnityEngine;
using UnityEngine.Pool;

public class GenericProjectileWeapon : Weapon
{
    [Header("Data")]
    [SerializeField] private WeaponDefinition definitionAsset;
    [SerializeField, Min(1)] private int startLevel = 1;
    [SerializeField] private bool aimAtMouse = true;

    private Camera cam;
    private GameObject ownerRoot;

    private IObjectPool<Projectile> pool;
    [SerializeField] private bool collectionCheck = true;
    [SerializeField] private int defaultCapacity = 64;
    [SerializeField] private int maxSize = 256;

    protected override void Awake()
    {
        base.Awake();
        cam = Camera.main;
        ownerRoot = transform.root ? transform.root.gameObject : gameObject;

        if (definitionAsset) SetDefinition(definitionAsset, startLevel);
        BuildPoolIfNeeded();
    }

    // PlayerShooting calls TryFire(dir). We only need to implement how to shoot.
    protected override void Shoot(Vector2 dir)
    {
        if (data == null) return;

        // Prefer mouse aim if requested
        if (aimAtMouse && cam && muzzle)
        {
            var m = cam.ScreenToWorldPoint(Input.mousePosition); m.z = 0f;
            var v = (Vector2)m - (Vector2)muzzle.position;
            if (v.sqrMagnitude > 0.0001f) dir = v.normalized;
        }

        if (!data.projectilePrefab)
            return;

        BuildPoolIfNeeded();

        var p = pool.Get();
        p.transform.position = muzzle ? muzzle.position : transform.position;

        // Configure teams & damage
        var hb = p.GetComponent<Damager>();
        if (hb) hb.Configure(ownerRoot, data.targetLayers, data.damage);

        // Initialise and push runtime settings
        p.Init(ownerRoot, data.targetLayers, data.damage, dir);

        float lifetime = data.projectileSpeed > 0.01f ? data.range / data.projectileSpeed : 999f;
        SetPrivate(p, "speed", data.projectileSpeed);
        SetPrivate(p, "lifetime", lifetime);
        SetPrivate(p, "obstacleLayers", data.obstacleLayers);
        SetPrivate(p, "maxPierces", data.maxPierces);
        SetPrivate(p, "pierceThroughObstacles", data.pierceThroughObstacles);
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

    private static void SetPrivate(object obj, string field, object value)
    {
        var f = obj.GetType().GetField(field,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (f != null) f.SetValue(obj, value);
    }
}
