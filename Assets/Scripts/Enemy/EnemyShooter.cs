using UnityEngine;
using UnityEngine.Pool;

public class EnemyShooter : MonoBehaviour
{
    [Header("Projectile")]
    [SerializeField] private BulletKinetic projectilePrefab;
    [SerializeField] private float projectileSpeed = 8f;
    [SerializeField] private float projectileDamage = 6f;
    [SerializeField] private float range = 10f; // projectile travel distance

    [Header("Fire Control")]
    [SerializeField] private float fireCooldown = 1.2f;
    float timer;

    [Header("Aim/Spawn")]
    [Tooltip("What actually rotates to aim (e.g., the enemy's hand/weapon root). If empty, uses this transform.")]
    [SerializeField] private Transform aimRoot;
    [Tooltip("Child transform at the weapon tip. Its local +X (right) must point out of the barrel.")]
    [SerializeField] private Transform muzzle;

    [Header("Masks")]
    [SerializeField] private LayerMask targetLayers;   // should include "Player"
    [SerializeField] private LayerMask wallLayers;

    Transform player;

    IObjectPool<BulletKinetic> pool;
    [SerializeField] bool collectionCheck = true;
    [SerializeField] int defaultCapacity = 64;
    [SerializeField] int maxSize = 512;

    void Awake()
    {
        pool = new ObjectPool<BulletKinetic>(
            Create, OnGet, OnRelease, OnDestroyPooled,
            collectionCheck, defaultCapacity, maxSize);
    }

    void Start()
    {
        player = PlayerController.Instance?.transform;
        if (!aimRoot) aimRoot = transform;
        if (targetLayers.value == 0) targetLayers = LayerMask.GetMask("Player");
    }

    // Aiming: point the mount at the player, but don't use that vector for firing
    void LateUpdate()
    {
        if (!player || !aimRoot) return;

        Vector3 pivot = muzzle ? muzzle.position : aimRoot.position;

        // Prefer the player's collider centre if available (less jitter than transform.position)
        Vector3 target = player.position;
        var pc = PlayerController.Instance;
        if (pc && pc.col) target = pc.col.bounds.center;

        Vector2 toTarget = (target - pivot);
        if (toTarget.sqrMagnitude > 0.0001f)
        {
            float angle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;
            aimRoot.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }
    }

    void Update()
    {
        if (!player) return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            Vector3 spawnPos = muzzle ? muzzle.position : transform.position;

            // 🔒 Direction is always the barrel's facing
            Vector2 dir = muzzle ? (Vector2)muzzle.right : (Vector2)transform.right;

            Fire(spawnPos, dir);
            timer = fireCooldown;
        }
    }

    void Fire(Vector3 spawnPos, Vector2 dir)
    {
        var b = pool.Get();

        // Align projectile sprite with the barrel
        Quaternion rot = muzzle ? muzzle.rotation
                                : Quaternion.AngleAxis(Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg, Vector3.forward);
        b.transform.SetPositionAndRotation(spawnPos, rot);

        // who to hurt + how much
        var dmg = b.GetComponent<Damager>();
        if (dmg) dmg.Configure(gameObject, targetLayers, projectileDamage);

        // move + range + despawn layers (BulletKinetic handles travel/despawn)
        b.Init(dir, projectileSpeed, range, wallLayers, targetLayers, pool);
    }

    // pool hooks
    BulletKinetic Create()                => Instantiate(projectilePrefab);
    void OnGet(BulletKinetic b)           => b.gameObject.SetActive(true);
    void OnRelease(BulletKinetic b)       => b.gameObject.SetActive(false);
    void OnDestroyPooled(BulletKinetic b) { if (b) Destroy(b.gameObject); }
}