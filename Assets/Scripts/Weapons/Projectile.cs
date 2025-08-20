using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

[RequireComponent(typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour
{
    [Header("Defaults (used if weapon doesn't override)")]
    [SerializeField] private float defaultSpeed = 18f;
    [SerializeField] private float defaultRange = 12f;
    [SerializeField] private LayerMask defaultTargetLayers;
    [SerializeField] private LayerMask defaultObstacleLayers;
    [SerializeField] private float defaultDamage = 5f;
    [SerializeField, Min(0f)] private float defaultRadius = 0.06f; // >0 uses CircleCast
    [SerializeField, Min(0f)] private float skin = 0.01f;          // back off from the surface slightly
    [SerializeField, Min(0)] private int defaultMaxPierces = 0;    // 0 = stop on first target
    [SerializeField] private bool defaultPierceThroughObstacles = false;
    [SerializeField] private GameObject defaultHitVFX;

    // Pool hook (set by spawner)
    public IObjectPool<Projectile> ObjectPool { get; set; }

    // Runtime (set by spawner each shot)
    private Rigidbody2D rb;
    private Transform ownerRoot;
    private Vector2 dir;

    private float speed;
    private float range;
    private LayerMask targetLayers;
    private LayerMask obstacleLayers;
    private float damage;
    private float radius;
    private int maxPierces;
    private bool pierceThroughObstacles;
    private GameObject hitVFX;

    private float despawnAt;
    private readonly HashSet<Collider2D> hitThisStep = new HashSet<Collider2D>();

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    /// <summary>
    /// Minimal init: owner/targets/damage/direction. Uses defaults for motion/behaviour until SetRuntime is called.
    /// </summary>
    public void Init(GameObject owner, LayerMask targets, float dmg, Vector2 direction)
    {
        ownerRoot = owner ? owner.transform.root : null;
        targetLayers = targets;
        damage = dmg;
        dir = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.right;

        // Start with defaults; weapon may override immediately after via SetRuntime()
        speed = defaultSpeed;
        range = defaultRange;
        damage = defaultDamage;
        obstacleLayers = defaultObstacleLayers;
        radius = Mathf.Max(0f, defaultRadius);
        maxPierces = Mathf.Max(0, defaultMaxPierces);
        pierceThroughObstacles = defaultPierceThroughObstacles;
        hitVFX = defaultHitVFX;

        // Compute lifetime from (range / speed)
        float lifetime = Mathf.Max(0.01f, range / Mathf.Max(0.01f, speed));
        despawnAt = Time.time + lifetime;

        // clear step cache for pooled reuse
        hitThisStep.Clear();
    }

    /// <summary>
    /// Optional runtime override from the weapon definition. Any nullable/optional args not supplied keep current values.
    /// Recomputes lifetime based on (range / speed).
    /// </summary>
    public void SetRuntime(
        float? speedOverride = null,
        float? rangeOverride = null,
        LayerMask? obstacleOverride = null,
        int? maxPiercesOverride = null,
        bool? pierceObstaclesOverride = null,
        float? radiusOverride = null,
        GameObject vfxOverride = null)
    {
        if (speedOverride.HasValue) speed = Mathf.Max(0.01f, speedOverride.Value);
        if (rangeOverride.HasValue) range = Mathf.Max(0f, rangeOverride.Value);
        if (obstacleOverride.HasValue) obstacleLayers = obstacleOverride.Value;
        if (maxPiercesOverride.HasValue) maxPierces = Mathf.Max(0, maxPiercesOverride.Value);
        if (pierceObstaclesOverride.HasValue) pierceThroughObstacles = pierceObstaclesOverride.Value;
        if (radiusOverride.HasValue) radius = Mathf.Max(0f, radiusOverride.Value);
        if (vfxOverride != null) hitVFX = vfxOverride;

        float lifetime = Mathf.Max(0.01f, range / Mathf.Max(0.01f, speed));
        despawnAt = Time.time + lifetime;
    }

    private void OnEnable()
    {
        hitThisStep.Clear();
    }

    private void FixedUpdate()
    {
        if (Time.time >= despawnAt) { Release(); return; }
        if (dir.sqrMagnitude < 0.0001f) { Release(); return; }

        Vector2 start = rb.position;
        Vector2 step  = dir * (speed * Time.fixedDeltaTime);
        float dist    = step.magnitude;
        if (dist <= Mathf.Epsilon) return;

        int mask = targetLayers | obstacleLayers;

        // Sweep the entire path for this step
        RaycastHit2D[] hits = (radius > 0f)
            ? Physics2D.CircleCastAll(start, radius, dir, dist + skin, mask)
            : Physics2D.RaycastAll(start, dir, dist + skin, mask);

        if (hits.Length == 0)
        {
            rb.MovePosition(start + step);
            return;
        }

        // Sort by distance so we process in travel order
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        hitThisStep.Clear();
        int pierces = 0;

        foreach (var hit in hits)
        {
            if (!hit.collider) continue;

            // Ignore self/owner (child colliders etc.)
            if (ownerRoot && hit.collider.transform.root == ownerRoot) continue;

            // Avoid double-processing the same collider in this step (possible with CircleCastAll)
            if (hitThisStep.Contains(hit.collider)) continue;
            hitThisStep.Add(hit.collider);

            int layer = hit.collider.gameObject.layer;
            bool isTarget   = (targetLayers.value   & (1 << layer)) != 0;
            bool isObstacle = (obstacleLayers.value & (1 << layer)) != 0;

            if (isTarget)
            {
                var dmgTarget = hit.collider.GetComponentInParent<IDamageable>();
                if (dmgTarget != null)
                {
                    dmgTarget.TakeDamage(damage);
                    SpawnVFX(hit);
                    pierces++;

                    if (pierces > maxPierces) // exceeded allowance → stop at this target
                    {
                        MoveToImpact(start, hit.distance);
                        Release();
                        return;
                    }
                    // else continue to check for further hits within this same step
                }
            }

            if (isObstacle && !pierceThroughObstacles)
            {
                MoveToImpact(start, hit.distance);
                Release();
                return;
            }
        }

        // If not blocked, keep going
        rb.MovePosition(start + step);
    }

    private void MoveToImpact(Vector2 start, float hitDistance)
    {
        float move = Mathf.Max(0f, hitDistance - skin);
        rb.MovePosition(start + dir * move);
    }

    private void SpawnVFX(RaycastHit2D hit)
    {
        if (!hitVFX) return;
        var go = Instantiate(hitVFX, hit.point, Quaternion.FromToRotation(Vector3.right, hit.normal));
        Destroy(go, 1.5f);
    }

    public void Release()
    {
        if (ObjectPool != null) ObjectPool.Release(this);
        else Destroy(gameObject);
    }
}