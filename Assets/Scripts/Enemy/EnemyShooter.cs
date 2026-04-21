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
    [SerializeField] private bool controlledExternally = false; // NEW: when true, FSM drives firing
    public void SetControlledExternally(bool value) => controlledExternally = value;
    float timer;
    public bool CanFire => timer <= 0f;

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
        if (!poolService) poolService = FindFirstObjectByType<UnityPoolService>(FindObjectsInactive.Include);
        if (!aimRoot) aimRoot = transform;
        if (targetLayers.value == 0) targetLayers = LayerMask.GetMask("Player");
    }

    void Start()
    {
        player = PlayerController.Instance ? PlayerController.Instance.transform : null;
    }

    void Update()
    {
        if (!player && PlayerController.Instance)
            player = PlayerController.Instance.transform;

        if (controlledExternally || !player) return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            Vector3 spawnPos = muzzle ? muzzle.position : transform.position;
            Vector2 dir = muzzle ? (Vector2)muzzle.right : (Vector2)transform.right;
            Fire(spawnPos, dir);
            timer = fireCooldown;
        }
    }

    void LateUpdate()
    {
        if (!player && PlayerController.Instance)
            player = PlayerController.Instance.transform;

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

    void Fire(Vector3 spawnPos, Vector2 dir)
    {
        if (!projectilePrefab)
            return;

        Quaternion rot = Quaternion.AngleAxis(Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg, Vector3.forward);

        GameObject go = poolService
            ? poolService.Spawn(projectilePrefab.gameObject, spawnPos, rot)
            : Instantiate(projectilePrefab.gameObject, spawnPos, rot);

        var p = go.GetComponent<Projectile>();
        if (!p)
        {
            Debug.LogWarning($"[EnemyShooter] Spawned object '{go.name}' has no Projectile component; adding one at runtime. Check pool prefab configuration.", go);
            p = go.AddComponent<Projectile>();
        }

        // Initialise projectile (Damager config is handled inside Projectile.Init as well)
        p.Init(gameObject, targetLayers, projectileDamage, dir);
        p.SetRuntime(
            speedOverride: projectileSpeed,
            rangeOverride: range,
            obstacleOverride: wallLayers
        );
    }

    /// <summary>Manual fire invoked by FSM; computes direction from muzzle/aimRoot.</summary>
    public bool FireAt(Vector3 worldPos)
    {
        if (timer > 0f || !aimRoot) return false;

        Vector3 pivot = muzzle ? muzzle.position : aimRoot.position;
        Vector2 dir   = ((Vector2)(worldPos - pivot)).normalized;
        Fire(pivot, dir);
        timer = fireCooldown;
        return true;
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
