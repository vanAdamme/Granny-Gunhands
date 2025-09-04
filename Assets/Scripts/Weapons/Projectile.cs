using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lightweight projectile. Counts unique target hits (for specials), respects obstacle layers,
/// and exposes Init/SetRuntime to match EnemyShooter. Damage is left to your combat pipeline.
/// </summary>
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

    private Transform     ownerRoot;
    private ISpecialCharge ownerCharge;

    // Optional bookkeeping if you later want damage here
    private float damage;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true; // typical top-down bullet behavior
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
        ownerCharge = null;
    }

    private void Update()
    {
        if (Time.time >= deathAt)
            Despawn();
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Public API expected by EnemyShooter (and weapons)
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Enemy path: sets owner, masks, base damage, and initial direction/velocity.
    /// Matches EnemyShooter usage.
    /// </summary>
    public void Init(GameObject ownerGO, LayerMask hitLayers, float baseDamage, Vector2 direction)
    {
        ownerRoot   = ownerGO ? ownerGO.transform.root : null;
        targetMask  = hitLayers;
        damage      = baseDamage;

        var dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
        rb.linearVelocity = dir * speed;
        transform.rotation = Quaternion.FromToRotation(Vector2.right, dir);
    }

    /// <summary>
    /// Enemy path: runtime overrides for speed, range→TTL, and obstacle layers.
    /// Matches EnemyShooter usage.
    /// </summary>
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

    /// <summary>
    /// Player weapon path: richer initialize that also injects the owner's special charge.
    /// </summary>
    public void Initialize(Transform owner,
                           ISpecialCharge charge,
                           Vector2 direction,
                           float setSpeed,
                           float ttlSeconds,
                           int   allowedUniqueHits,
                           LayerMask hitMask,
                           LayerMask blockMask,
                           bool  canPierceObstacles)
    {
        ownerRoot            = owner ? owner.root : null;
        ownerCharge          = charge;
        speed                = setSpeed > 0f ? setSpeed : speed;
        timeToLive           = ttlSeconds > 0f ? ttlSeconds : timeToLive;
        deathAt              = Time.time + timeToLive;
        maxUniqueHits        = Mathf.Max(1, allowedUniqueHits);
        targetMask           = hitMask;
        obstructionMask      = blockMask;
        pierceThroughObstacles = canPierceObstacles;

        var dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
        rb.linearVelocity = dir * speed;
        transform.rotation = Quaternion.FromToRotation(Vector2.right, dir);

        uniqueHitsSoFar = 0;
        hitRoots.Clear();
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Collision & counting
    // ─────────────────────────────────────────────────────────────────────────────

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

        // ensure "genuine" first-time hit per-root
        var root = other.transform.root;
        int rootId = root.GetInstanceID();
        if (!hitRoots.Add(rootId)) return;

        // Optional: forward to your damage system here if desired.

        // Charge special on successful contact
        ownerCharge?.AddHits(1);

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
        // Swap to a pool.Release(this) when you introduce pooling.
        Destroy(gameObject);
    }

    // Optional helpers
    public void SetHitMask(LayerMask m) => targetMask = m;
    public void SetObstructionMask(LayerMask m) => obstructionMask = m;
    public void SetPierceObstacles(bool v) => pierceThroughObstacles = v;
}