using UnityEngine;
using UnityEngine.Pool;

public class Shotgun : Weapon
{
    [Header("Projectile")]
    [SerializeField] private BulletKinetic projectilePrefab;
    [SerializeField] private float projectileSpeed = 14f;
    [SerializeField] private float damagePerPellet = 0.9f;
    [SerializeField] private float range = 10f;

    [Header("Spread")]
    [SerializeField, Min(1)] private int pelletCount = 6;
    [SerializeField, Range(0f, 45f)] private float spreadAngle = 18f; // total cone

    [Header("Hit Masks")]
    [SerializeField] private LayerMask targetLayers;   // e.g. "Enemy"
    [SerializeField] private LayerMask wallLayers;     // e.g. "Walls"

    // Pool (one pool handles all pellets)
    private IObjectPool<BulletKinetic> pool;
    [SerializeField] private bool collectionCheck = true;
    [SerializeField] private int defaultCapacity = 64;
    [SerializeField] private int maxSize = 512;

    protected override void Awake()
    {
        base.Awake();
        pool = new ObjectPool<BulletKinetic>(Create, OnGet, OnRelease, OnDestroyPooled,
                                             collectionCheck, defaultCapacity, maxSize);
    }

    protected override void Shoot(Vector2 _ /* ignored */)
    {
        // Base forward from the barrel's +X
        Vector3 spawnPos = muzzlePosition ? muzzlePosition.position : transform.position;
        Vector2 forward  = muzzlePosition ? (Vector2)muzzlePosition.right : (Vector2)transform.right;
        Quaternion baseRot = muzzlePosition ? muzzlePosition.rotation
                                            : Quaternion.AngleAxis(Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg, Vector3.forward);

        float half = spreadAngle * 0.5f;

        for (int i = 0; i < pelletCount; i++)
        {
            // 0..1 across the arc, centred on forward
            float t = pelletCount == 1 ? 0f : i / (float)(pelletCount - 1);
            float angle = Mathf.Lerp(-half, +half, t);

            // Rotate from muzzle forward by 'angle'
            Vector2 pelletDir = Quaternion.AngleAxis(angle, Vector3.forward) * forward;
            Quaternion pelletRot = baseRot * Quaternion.AngleAxis(angle, Vector3.forward);

            FirePellet(spawnPos, pelletDir, pelletRot);
        }
    }

    private void FirePellet(Vector3 spawnPos, Vector2 dir, Quaternion rot)
    {
        var b = pool.Get();
        b.transform.SetPositionAndRotation(spawnPos, rot);

        var hb = b.GetComponent<Damager>();
        if (hb) hb.Configure(ownerRoot ? ownerRoot : gameObject, targetLayers, damagePerPellet);

        b.Init(dir, projectileSpeed, range, wallLayers, targetLayers, pool);
    }

    // Pool hooks
    private BulletKinetic Create()               => Instantiate(projectilePrefab);
    private void OnGet(BulletKinetic b)         => b.gameObject.SetActive(true);
    private void OnRelease(BulletKinetic b)     => b.gameObject.SetActive(false);
    private void OnDestroyPooled(BulletKinetic b){ if (b) Destroy(b.gameObject); }

    private static Vector2 Rotate2D(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cs = Mathf.Cos(rad);
        float sn = Mathf.Sin(rad);
        return new Vector2(v.x * cs - v.y * sn, v.x * sn + v.y * cs);
    }
}