using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour
{
    [Header("Defaults (used if weapon doesn't override)")]
    [SerializeField] private float     defaultSpeed  = 18f;
    [SerializeField] private float     defaultRange  = 12f;
    [SerializeField] private LayerMask defaultTargetLayers;
    [SerializeField] private LayerMask defaultObstacleLayers;
    [SerializeField] private float     defaultDamage = 5f;
    [SerializeField, Min(0f)] private float defaultRadius = 0.06f; // >0 uses CircleCast
    [SerializeField, Min(0f)] private float skin = 0.01f;          // back off from surface
    [SerializeField, Min(0)]  private int   defaultMaxPierces = 0; // 0 = stop on first target
    [SerializeField] private bool  defaultPierceThroughObstacles = false;
    [SerializeField] private GameObject defaultHitVFX;

    [Header("Audio (defaults)")]
    [SerializeField] private SoundEvent defaultTargetHitSfx;   // played when hitting a damageable
    [SerializeField] private SoundEvent defaultObstacleHitSfx; // played when hitting a wall/obstacle

    [Header("Services (optional)")]
    [SerializeField] private UnityPoolService poolService; // optional; will auto-find

    // Runtime state
    private Rigidbody2D rb;
    private Transform   ownerRoot;
    private Vector2     dir;

    private float     speed;
    private float     range;
    private LayerMask targetLayers;
    private LayerMask obstacleLayers;
    private float     damage;
    private float     radius;
    private int       maxPierces;
    private bool      pierceThroughObstacles;
    private GameObject hitVFX;

    // Audio runtime overrides (optional)
    private SoundEvent targetHitSfx;
    private SoundEvent obstacleHitSfx;

    private float despawnAt;
    private readonly HashSet<Collider2D> hitThisStep = new HashSet<Collider2D>();

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        // Use the non-obsolete finder as a fallback (Unity 6+)
        if (!poolService) poolService = FindFirstObjectByType<UnityPoolService>();
        if (!GetComponent<PooledObject>()) gameObject.AddComponent<PooledObject>(); // ensure pooled return helper
    }

    /// <summary>Called by the spawner immediately after Spawn()</summary>
    public void Init(GameObject owner, LayerMask targets, float dmg, Vector2 direction)
    {
        ownerRoot = owner ? owner.transform.root : null;
        dir = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.right;

        // start from defaults
        speed      = Mathf.Max(0.01f, defaultSpeed);
        range      = Mathf.Max(0f,     defaultRange);
        targetLayers   = (targets.value != 0) ? targets : defaultTargetLayers;
        obstacleLayers = defaultObstacleLayers;
        damage     = dmg > 0f ? dmg : defaultDamage;
        radius     = Mathf.Max(0f, defaultRadius);
        maxPierces = Mathf.Max(0,  defaultMaxPierces);
        pierceThroughObstacles = defaultPierceThroughObstacles;
        hitVFX     = defaultHitVFX;

        // audio defaults
        targetHitSfx   = defaultTargetHitSfx;
        obstacleHitSfx = defaultObstacleHitSfx;

        // lifetime from range / speed
        float lifetime = Mathf.Max(0.01f, range / Mathf.Max(0.01f, speed));
        despawnAt = Time.time + lifetime;

        hitThisStep.Clear();

        // Optional: configure your Damager if present
        var damager = GetComponent<Damager>();
        if (damager) damager.Configure(ownerRoot ? ownerRoot.gameObject : null, targetLayers, damage);
    }

    /// <summary>Optional runtime overrides from a weapon/definition.</summary>
    public void SetRuntime(
        float?     speedOverride   = null,
        float?     rangeOverride   = null,
        LayerMask? obstacleOverride = null,
        int?       maxPiercesOverride = null,
        bool?      pierceObstaclesOverride = null,
        float?     radiusOverride  = null,
        GameObject vfxOverride     = null,
        SoundEvent targetHitSfxOverride   = null,
        SoundEvent obstacleHitSfxOverride = null)
    {
        if (speedOverride.HasValue)        speed = Mathf.Max(0.01f, speedOverride.Value);
        if (rangeOverride.HasValue)        range = Mathf.Max(0f,     rangeOverride.Value);
        if (obstacleOverride.HasValue)     obstacleLayers = obstacleOverride.Value;
        if (maxPiercesOverride.HasValue)   maxPierces = Mathf.Max(0, maxPiercesOverride.Value);
        if (pierceObstaclesOverride.HasValue) pierceThroughObstacles = pierceObstaclesOverride.Value;
        if (radiusOverride.HasValue)       radius = Mathf.Max(0f,    radiusOverride.Value);
        if (vfxOverride != null)           hitVFX = vfxOverride;

        if (targetHitSfxOverride)   targetHitSfx   = targetHitSfxOverride;
        if (obstacleHitSfxOverride) obstacleHitSfx = obstacleHitSfxOverride;

        float lifetime = Mathf.Max(0.01f, range / Mathf.Max(0.01f, speed));
        despawnAt = Time.time + lifetime;
    }

    // Message sent by UnityPoolService when this is taken from the pool
    void OnSpawnedFromPool()
    {
        hitThisStep.Clear();
        rb.linearVelocity = Vector2.zero; // Unity 6 2D API
        var trail = GetComponent<TrailRenderer>();
        if (trail) trail.Clear();
    }

    void FixedUpdate()
    {
        if (Time.time >= despawnAt) { Release(); return; }
        if (dir.sqrMagnitude < 0.0001f) { Release(); return; }

        Vector2 start = rb.position;
        Vector2 step  = dir * (speed * Time.fixedDeltaTime);
        float   dist  = step.magnitude;
        if (dist <= Mathf.Epsilon) return;

        int mask = targetLayers | obstacleLayers;

        RaycastHit2D[] hits = (radius > 0f)
            ? Physics2D.CircleCastAll(start, radius, dir, dist + skin, mask)
            : Physics2D.RaycastAll(start,          dir, dist + skin, mask);

        if (hits.Length == 0)
        {
            rb.MovePosition(start + step);
            return;
        }

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        hitThisStep.Clear();
        int pierces = 0;

        foreach (var hit in hits)
        {
            if (!hit.collider) continue;

            if (ownerRoot && hit.collider.transform.root == ownerRoot) continue;
            if (hitThisStep.Contains(hit.collider)) continue;
            hitThisStep.Add(hit.collider);

            int  layer      = hit.collider.gameObject.layer;
            bool isTarget   = (targetLayers.value   & (1 << layer)) != 0;
            bool isObstacle = (obstacleLayers.value & (1 << layer)) != 0;

            if (isTarget)
            {
                var dmgTarget = hit.collider.GetComponentInParent<IDamageable>();
                if (dmgTarget != null)
                {
                    dmgTarget.TakeDamage(damage);
                    SpawnVFX(hit);
                    PlaySfxAt(hit.point, targetHitSfx);
                    pierces++;

                    if (pierces > maxPierces)
                    {
                        MoveToImpact(start, hit.distance);
                        Release();
                        return;
                    }
                }
            }

            if (isObstacle && !pierceThroughObstacles)
            {
                SpawnVFX(hit);
                PlaySfxAt(hit.point, obstacleHitSfx ? obstacleHitSfx : targetHitSfx);
                MoveToImpact(start, hit.distance);
                Release();
                return;
            }
        }

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
        var rot = Quaternion.FromToRotation(Vector3.right, hit.normal);
        if (poolService) poolService.Spawn(hitVFX, hit.point, rot);
        else Destroy(Instantiate(hitVFX, hit.point, rot), 1.5f);
    }

    private void PlaySfxAt(Vector2 worldPos, SoundEvent evt)
    {
        if (!evt) return;
        AudioServicesProvider.Audio?.Play(evt, worldPos);
    }

    private void Release()
    {
        var po = GetComponent<PooledObject>();
        if (po != null) po.Release();
        else Destroy(gameObject);
    }
}