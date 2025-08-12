using UnityEngine;
using UnityEngine.Pool;

public class Pistol : Weapon
{
    [Header("Projectile")]
    [SerializeField] private BulletKinetic projectilePrefab;
    [SerializeField] private float projectileSpeed = 18f;
    [SerializeField] private float damage = 1f;
    [SerializeField] private float range = 12f;

    [Header("Hit Masks")]
    [SerializeField] private LayerMask targetLayers;
    [SerializeField] private LayerMask wallLayers;

    // Pool
    IObjectPool<BulletKinetic> pool;
    [SerializeField] bool collectionCheck = true;
    [SerializeField] int defaultCapacity = 64;
    [SerializeField] int maxSize = 512;

    protected override void Awake()
    {
        base.Awake();
        pool = new ObjectPool<BulletKinetic>(
            Create, OnGet, OnRelease, OnDestroyPooled,
            collectionCheck, defaultCapacity, maxSize);
    }

    protected override void Shoot(Vector2 dir)
    {
        var b = pool.Get();
        b.transform.position = muzzlePosition.position;

        var hb = b.GetComponent<Damager>();
        if (hb) hb.Configure(ownerRoot, targetLayers, damage);

        b.Init(dir, projectileSpeed, range, wallLayers, targetLayers, pool);
    }

    BulletKinetic Create() => Instantiate(projectilePrefab);
    void OnGet(BulletKinetic b)     => b.gameObject.SetActive(true);
    void OnRelease(BulletKinetic b) => b.gameObject.SetActive(false);
    void OnDestroyPooled(BulletKinetic b) { if (b) Destroy(b.gameObject); }
}