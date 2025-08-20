using UnityEngine;
using UnityEngine.Pool;

public class EnemyShooter : MonoBehaviour
{
    [Header("Projectile")]
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private float projectileSpeed = 8f;
    [SerializeField] private float projectileDamage = 6f;
    [SerializeField] private float range = 10f;

    [Header("Fire Control")]
    [SerializeField] private float fireCooldown = 1.2f;
    float timer;

    [Header("Aim/Spawn")]
    [SerializeField] private Transform aimRoot;
    [SerializeField] private Transform muzzle;

    [Header("Masks")]
    [SerializeField] private LayerMask targetLayers;
    [SerializeField] private LayerMask wallLayers;

    Transform player;

    IObjectPool<Projectile> pool;
    [SerializeField] bool collectionCheck = true;
    [SerializeField] int defaultCapacity = 64;
    [SerializeField] int maxSize = 512;

    void Awake()
    {
        pool = new ObjectPool<Projectile>(
            Create, OnGet, OnRelease, OnDestroyPooled,
            collectionCheck, defaultCapacity, maxSize);
    }

    void Start()
    {
        player = PlayerController.Instance?.transform;
        if (!aimRoot) aimRoot = transform;
        if (targetLayers.value == 0) targetLayers = LayerMask.GetMask("Player");
    }

    void LateUpdate()
    {
        if (!player || !aimRoot) return;
        Vector3 pivot = muzzle ? muzzle.position : aimRoot.position;

        Vector3 target = player.position;
        var pc = PlayerController.Instance;
        if (pc && pc.col) target = pc.col.bounds.center;

        Vector2 toTarget = target - pivot;
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
            Vector2 dir = muzzle ? (Vector2)muzzle.right : (Vector2)transform.right;
            Fire(spawnPos, dir);
            timer = fireCooldown;
        }
    }

    void Fire(Vector3 spawnPos, Vector2 dir)
    {
        var p = pool.Get();
        p.transform.SetPositionAndRotation(spawnPos,
            muzzle ? muzzle.rotation : Quaternion.AngleAxis(Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg, Vector3.forward));

        // configure hit + damage
        var dmg = p.GetComponent<Damager>();
        if (dmg) dmg.Configure(gameObject, targetLayers, projectileDamage);

        // initialise projectile
        p.Init(gameObject, targetLayers, projectileDamage, dir);
        p.SetRuntime(
            speedOverride: projectileSpeed,
            rangeOverride: range,
            obstacleOverride: wallLayers
        );
        p.ObjectPool = pool;
    }

    // Pool hooks
    Projectile Create() => Instantiate(projectilePrefab);
    void OnGet(Projectile p) => p.gameObject.SetActive(true);
    void OnRelease(Projectile p) => p.gameObject.SetActive(false);
    void OnDestroyPooled(Projectile p) { if (p) Destroy(p.gameObject); }
}