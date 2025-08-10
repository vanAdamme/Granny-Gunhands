using UnityEngine;
using UnityEngine.Pool;

public class EnemyShooter : MonoBehaviour
{
    [SerializeField] private BulletKinetic projectilePrefab;
    [SerializeField] private float projectileSpeed = 8f;
    [SerializeField] private float projectileDamage = 6f;
    [SerializeField] private float range = 10f;
    [SerializeField] private float fireCooldown = 1.2f;

    [Header("Aim/Spawn")]
    [SerializeField] private Transform muzzle;  // assign a child at the weapon tip

    [Header("Masks")]
    [SerializeField] private LayerMask targetLayers;   // should include "Player"
    [SerializeField] private LayerMask wallLayers;

    float timer;
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

        // Safety: default the mask if not set in the Inspector
        if (targetLayers.value == 0)
            targetLayers = LayerMask.GetMask("Player");
    }

    void Update()
    {
        if (!player) return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            Vector3 spawnPos = muzzle ? muzzle.position : transform.position;

            // Aim at the player's collider center (falls back to transform.position)
            var pc = PlayerController.Instance;
            Vector3 aimPos = (pc && pc.col) ? (Vector3)pc.col.bounds.center : player.position;

            Vector2 dir = (aimPos - spawnPos).sqrMagnitude > 0.0001f
                ? (Vector2)(aimPos - spawnPos).normalized
                : Vector2.right;

            Fire(spawnPos, dir);
            timer = fireCooldown;
        }
    }

    void Fire(Vector3 spawnPos, Vector2 dir)
    {
        var b = pool.Get();
        b.transform.SetPositionAndRotation(spawnPos,
            Quaternion.AngleAxis(Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg, Vector3.forward));

        // who to hurt + how much
        var dmg = b.GetComponent<Damager>();
        if (dmg) dmg.Configure(gameObject, targetLayers, projectileDamage);

        // move + range + despawn layers
        b.Init(dir, projectileSpeed, range, wallLayers, targetLayers, pool);
    }

    // pool hooks
    BulletKinetic Create()                => Instantiate(projectilePrefab);
    void OnGet(BulletKinetic b)           => b.gameObject.SetActive(true);
    void OnRelease(BulletKinetic b)       => b.gameObject.SetActive(false);
    void OnDestroyPooled(BulletKinetic b) { if (b) Destroy(b.gameObject); }
}