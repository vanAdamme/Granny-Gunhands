using UnityEngine;
using UnityEngine.Pool;

public class Shotgun : Weapon
{
    [Header("Projectile")]
    [SerializeField] private BulletKinetic projectilePrefab;
    [SerializeField] private float projectileSpeed = 16f;
    [SerializeField] private float range = 10f;

    [Header("Pellets")]
    [SerializeField, Min(1)] private int pelletCount = 8;
    [SerializeField, Min(0f)] private float spreadAngle = 24f;     // total cone in degrees
    [SerializeField, Min(0f)] private float randomJitter = 3f;     // extra randomness per pellet
    [SerializeField] private float pelletDamage = 0.9f;            // damage per pellet

    [Header("Hit Masks")]
    [SerializeField] private LayerMask targetLayers;
    [SerializeField] private LayerMask wallLayers;

    // Pool (one pool for pellets of this shotgun)
    private IObjectPool<BulletKinetic> pool;
    [SerializeField] private bool collectionCheck = true;
    [SerializeField] private int defaultCapacity = 128;
    [SerializeField] private int maxSize = 1024;

    protected override void Awake()
    {
        base.Awake();
        pool = new ObjectPool<BulletKinetic>(
            Create, OnGet, OnRelease, OnDestroyPooled,
            collectionCheck, defaultCapacity, maxSize);
    }

    protected override void Shoot(Vector2 dir)
    {
        if (!muzzlePosition) muzzlePosition = transform; // fallback

        // Base angle from the requested direction
        float baseDeg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        // Evenly distribute pellets across the spread cone
        float halfSpread = spreadAngle * 0.5f;
        for (int i = 0; i < pelletCount; i++)
        {
            // t in [-1, 1] across pellets
            float t = pelletCount == 1 ? 0f : (i / (pelletCount - 1f)) * 2f - 1f;
            float evenOffset = t * halfSpread;
            float jitter = (randomJitter > 0f) ? Random.Range(-randomJitter, randomJitter) : 0f;

            float finalDeg = baseDeg + evenOffset + jitter;
            Vector2 pelletDir = DegToDir(finalDeg);

            var b = pool.Get();
            b.transform.position = muzzlePosition.position;

            // Configure damage + team
            var hb = b.GetComponent<Damager>();
            if (hb) hb.Configure(ownerRoot, targetLayers, pelletDamage);

            // Launch pellet
            b.Init(pelletDir, projectileSpeed, range, wallLayers, targetLayers, pool);
        }
    }

    // Helpers
    private static Vector2 DegToDir(float deg)
    {
        float rad = deg * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
    }

    private BulletKinetic Create() => Instantiate(projectilePrefab);
    private void OnGet(BulletKinetic b)     => b.gameObject.SetActive(true);
    private void OnRelease(BulletKinetic b) => b.gameObject.SetActive(false);
    private void OnDestroyPooled(BulletKinetic b) { if (b) Destroy(b.gameObject); }
}