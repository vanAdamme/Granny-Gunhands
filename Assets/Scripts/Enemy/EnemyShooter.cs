using UnityEngine;

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

    [Header("Services")]
    [SerializeField] private UnityPoolService poolService;

    Transform player;

    void Awake()
    {
        if (!poolService) poolService = FindFirstObjectByType<UnityPoolService>(); // Unity 6-safe
        if (!aimRoot) aimRoot = transform;
        if (targetLayers.value == 0) targetLayers = LayerMask.GetMask("Player");
    }

    void Start()
    {
        player = PlayerController.Instance ? PlayerController.Instance.transform : null;
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
        if (!projectilePrefab)
            return;

        Quaternion rot = Quaternion.AngleAxis(Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg, Vector3.forward);

        GameObject go = poolService
            ? poolService.Spawn(projectilePrefab.gameObject, spawnPos, rot)
            : Instantiate(projectilePrefab.gameObject, spawnPos, rot);

        var p = go.GetComponent<Projectile>();
        if (!p) p = go.AddComponent<Projectile>();

        // Initialise projectile (Damager config is handled inside Projectile.Init as well)
        p.Init(gameObject, targetLayers, projectileDamage, dir);
        p.SetRuntime(
            speedOverride: projectileSpeed,
            rangeOverride: range,
            obstacleOverride: wallLayers
        );
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!muzzle) return;
        UnityEditor.Handles.color = new Color(1f, 0.4f, 0.1f, 0.5f);
        UnityEditor.Handles.DrawSolidDisc(muzzle.position, Vector3.forward, 0.05f);
    }
#endif
}
