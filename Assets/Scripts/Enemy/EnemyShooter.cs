using UnityEngine;
using UnityEngine.Pool;

public class EnemyShooter : MonoBehaviour
{
    [SerializeField] private BulletKinetic projectilePrefab;
    [SerializeField] private float projectileSpeed = 8f;
    [SerializeField] private float projectileDamage = 6f;
    [SerializeField] private float range = 10f;
    [SerializeField] private float fireCooldown = 1.2f;

    [Header("Masks")]
    [SerializeField] private LayerMask targetLayers;
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
    }

    void Update()
    {
        if (!player) return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            Vector2 dir = (player.position - transform.position).normalized;
            Fire(dir);
            timer = fireCooldown;
        }
    }

    void Fire(Vector2 dir)
    {
        var b = pool.Get();
        b.transform.position = transform.position;

        // who to hurt + how much
        var hb = b.GetComponent<Damager>();
        if (hb) hb.Configure(gameObject, targetLayers, projectileDamage);

        // move + range + despawn layers
        b.Init(dir, projectileSpeed, range, wallLayers, targetLayers, pool);
    }

    // pool hooks
    BulletKinetic Create()                  => Instantiate(projectilePrefab);
    void OnGet(BulletKinetic b)             => b.gameObject.SetActive(true);
    void OnRelease(BulletKinetic b)         => b.gameObject.SetActive(false);
    void OnDestroyPooled(BulletKinetic b)   { if (b) Destroy(b.gameObject); }
}