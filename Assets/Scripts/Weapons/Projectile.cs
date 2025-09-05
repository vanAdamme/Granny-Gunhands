using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class Projectile : MonoBehaviour
{
    [Header("Runtime (injected)")]
    [SerializeField] private float speed = 12f;
    [SerializeField] private float timeToLive = 1.0f;
    [SerializeField] private int   maxUniqueHits = 1;             // 1 = no pierce, 2 = one pierce, etc.
    [SerializeField] private LayerMask targetMask;
    [SerializeField] private LayerMask obstructionMask;
    [SerializeField] private bool  pierceThroughObstacles = false;

    [Header("FX (optional)")]
    [SerializeField] private GameObject hitVfxPrefab;
    [SerializeField] private float      vfxLifetime = 1.2f;

    private Rigidbody2D rb;
    private float deathAt;
    private int   uniqueHitsSoFar;
    private HashSet<int> hitRoots;

    private Transform ownerRoot;

    // damage + special charge hooks
    private float damage;
    private ISpecialCharge charger;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;
        hitRoots = new HashSet<int>(8);
    }

    private void OnEnable()
    {
        deathAt = Time.time + timeToLive;
        uniqueHitsSoFar = 0;
        hitRoots.Clear();
    }

    private void OnDisable()
    {
        ownerRoot = null;
        charger = null;
    }

    private void Update()
    {
        if (Time.time >= deathAt)
            Despawn();
    }

    // ───────────────── EnemyShooter path ─────────────────
    public void Init(GameObject ownerGO, LayerMask hitLayers, float baseDamage, Vector2 direction)
    {
        ownerRoot  = ownerGO ? ownerGO.transform.root : null;
        targetMask = hitLayers;
        damage     = baseDamage;

        var dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
        rb.linearVelocity = dir * speed;
        transform.rotation = Quaternion.FromToRotation(Vector2.right, dir);
    }

    public void SetRuntime(float speedOverride, float rangeOverride, LayerMask obstacleOverride)
    {
        if (speedOverride > 0f) speed = speedOverride;
        if (rangeOverride > 0f)
        {
            float ttl = Mathf.Max(0.01f, rangeOverride / Mathf.Max(0.01f, speed));
            timeToLive = ttl;
            deathAt = Time.time + timeToLive;
        }
        obstructionMask = obstacleOverride;
    }

    // ───────────────── Player weapon path ─────────────────
    public void Initialize(Transform owner,
                           ISpecialCharge charge,
                           Vector2 direction,
                           float setSpeed,
                           float ttlSeconds,
                           int   allowedUniqueHits,
                           LayerMask hitMask,
                           LayerMask blockMask,
                           bool  canPierceObstacles,
                           float dmg)                    // ← NEW: damage for player bullets
    {
        ownerRoot            = owner ? owner.root : null;
        charger              = charge;
        speed                = setSpeed > 0f ? setSpeed : speed;
        timeToLive           = ttlSeconds > 0f ? ttlSeconds : timeToLive;
        deathAt              = Time.time + timeToLive;
        maxUniqueHits        = Mathf.Max(1, allowedUniqueHits);
        targetMask           = hitMask;
        obstructionMask      = blockMask;
        pierceThroughObstacles = canPierceObstacles;
        damage               = Mathf.Max(0f, dmg);

        var dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
        rb.linearVelocity = dir * speed;
        transform.rotation = Quaternion.FromToRotation(Vector2.right, dir);

        uniqueHitsSoFar = 0;
        hitRoots.Clear();
    }

    // Optional helper for wiring charge separately
    public void SetCharger(ISpecialCharge c) => charger = c;

    // ───────────────── Collisions & counting ─────────────────
    private void OnTriggerEnter2D(Collider2D other)
    {
        // ignore self
        if (ownerRoot && other.transform.root == ownerRoot) return;

        int otherLayerMask = 1 << other.gameObject.layer;

        // obstruction handling (e.g., walls)
        if (!pierceThroughObstacles && (obstructionMask.value & otherLayerMask) != 0)
        {
            SpawnHitVfx(other.ClosestPoint(transform.position));
            Despawn();
            return;
        }

        // not a target layer → ignore
        if ((targetMask.value & otherLayerMask) == 0) return;

        // ensure first-time hit per-root
        var root = other.transform.root;
        int rootId = root.GetInstanceID();
        if (!hitRoots.Add(rootId)) return;

        // Apply damage if possible
        var dmgTarget = other.GetComponentInParent<IDamageable>();
        if (dmgTarget != null && damage > 0f)
        {
            dmgTarget.TakeDamage(damage);
            // count charge only when damage is actually applied
            charger?.AddHits(1);

            // only count player bullets
            if (ownerRoot && ownerRoot.GetComponent<PlayerController>())
                PlayerDamageEvents.Report(damage);
        }

        uniqueHitsSoFar++;
        SpawnHitVfx(other.ClosestPoint(transform.position));

        if (uniqueHitsSoFar >= maxUniqueHits)
            Despawn();
    }

    private void SpawnHitVfx(Vector2 at)
    {
        if (!hitVfxPrefab) return;
        var vfx = Instantiate(hitVfxPrefab, at, Quaternion.identity);
        Destroy(vfx, vfxLifetime);
    }

    private void Despawn()
    {
        Destroy(gameObject); // swap for pool.Release(this) if pooled
    }

    // Optional setters
    public void SetHitMask(LayerMask m) => targetMask = m;
    public void SetObstructionMask(LayerMask m) => obstructionMask = m;
    public void SetPierceObstacles(bool v) => pierceThroughObstacles = v;
}