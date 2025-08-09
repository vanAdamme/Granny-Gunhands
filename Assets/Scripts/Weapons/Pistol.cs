using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Pool;

public class Pistol : MonoBehaviour
{
    [Header("Projectile")]
    [SerializeField] private BulletKinetic projectilePrefab;   // prefab has: Rigidbody2D + CircleCollider2D(isTrigger) + Hitbox + BulletKinetic
    [SerializeField] private Transform muzzlePosition;

    [Header("Firing")]
    [SerializeField] private float cooldownWindow = 0.1f;
    [SerializeField] private float projectileSpeed = 18f;
    [SerializeField] private float damage = 1f;
    [SerializeField] private float range = 12f;

    [Header("Targeting")]
    [SerializeField] private LayerMask targetLayers;           // e.g. Enemy
    [SerializeField] private LayerMask wallLayers;             // e.g. Walls
    [SerializeField] private bool aimAtMouse = true;

    [Header("UI/FX")]
    [SerializeField] public Sprite icon;
    [SerializeField] private UnityEvent m_GunFired;

    Camera cam;
    float nextFire;
    GameObject ownerRoot;

    // Pool
    IObjectPool<BulletKinetic> pool;
    [SerializeField] bool collectionCheck = true;
    [SerializeField] int defaultCapacity = 64;
    [SerializeField] int maxSize = 512;

    void Awake()
    {
        cam = Camera.main;
        ownerRoot = transform.root.gameObject;

        pool = new ObjectPool<BulletKinetic>(
            Create, OnGet, OnRelease, OnDestroyPooled,
            collectionCheck, defaultCapacity, maxSize);
    }

    void Update()
    {
        if (Time.time < nextFire || !Input.GetMouseButton(0)) return;

        Vector2 dir;
        if (aimAtMouse && cam)
        {
            var m = cam.ScreenToWorldPoint(Input.mousePosition); m.z = 0f;
            dir = ((Vector2)m - (Vector2)muzzlePosition.position).normalized;
            if (dir.sqrMagnitude < 0.0001f) dir = muzzlePosition.right;
        }
        else dir = muzzlePosition.right;

          Fire(dir);
        nextFire = Time.time + cooldownWindow;
    }

    void Fire(Vector2 dir)
    {
        var b = pool.Get();
        b.transform.position = muzzlePosition.position;

        // Configure hitbox damage/teams
        var hb = b.GetComponent<Damager>();
        if (hb) hb.Configure(ownerRoot, targetLayers, damage);

        // Kick it and set its range & despawn layers
        b.Init(dir, projectileSpeed, range, wallLayers, targetLayers, pool);

        m_GunFired?.Invoke();
    }

    // Pool hooks
    BulletKinetic Create()
    {
        var go = Instantiate(projectilePrefab);
        return go;
    }
    void OnGet(BulletKinetic b)   { b.gameObject.SetActive(true); }
    void OnRelease(BulletKinetic b){ b.gameObject.SetActive(false); }
    void OnDestroyPooled(BulletKinetic b){ if (b) Destroy(b.gameObject); }
}