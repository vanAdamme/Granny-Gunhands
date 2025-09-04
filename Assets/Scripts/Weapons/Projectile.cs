using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lightweight projectile that travels with a Rigidbody2D, reports genuine contacts with Targets,
/// and optionally pierces multiple unique targets. Damage application is deliberately not handled
/// here to keep responsibilities narrow; your existing damage pipeline can stay as-is.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class Projectile : MonoBehaviour
{
    [Header("Motion")]
    [SerializeField] private float speed = 20f;
    [SerializeField] private float lifetime = 4f;

    [Header("Hit Rules")]
    [Tooltip("Layers considered hittable (e.g., Enemies).")]
    [SerializeField] private LayerMask hitMask;
    [Tooltip("How many DISTINCT targets this projectile can hit before it despawns. 1 = no pierce.")]
    [SerializeField] private int maxUniqueHits = 1;
    [Tooltip("If true, the projectile is destroyed when it first touches anything not in hitMask (e.g., walls).")]
    [SerializeField] private bool dieOnObstruction = true;
    [SerializeField] private LayerMask obstructionMask;

    [Header("FX")]
    [SerializeField] private GameObject hitVfxPrefab;
    [SerializeField] private GameObject muzzleTrailPrefab;
    [SerializeField] private float vfxLifetime = 1.2f;

    // Runtime
    private Rigidbody2D rb;
    private float deathAt;
    private int uniqueHitsSoFar;
    private HashSet<int> hitIds; // to avoid double-counting the same collider/root
    private Transform ownerRoot;
    private ISpecialCharge ownerCharge;

    // Optional cached components
    private GameObject spawnedTrail;

    /// <summary>
    /// Call on spawn/pull to configure the projectile. Direction is normalized internally.
    /// </summary>
    public void Initialize(Transform owner, ISpecialCharge charge, Vector2 direction, float initialSpeed = -1f, float timeToLive = -1f, int pierceCount = -1)
    {
        ownerRoot = owner ? owner.root : null;
        ownerCharge = charge;

        if (initialSpeed > 0f) speed = initialSpeed;
        if (timeToLive > 0f) lifetime = timeToLive;
        if (pierceCount >= 1) maxUniqueHits = pierceCount;

        rb.velocity = direction.sqrMagnitude > 0.0001f ? direction.normalized * speed : rb.velocity;
        deathAt = Time.time + lifetime;
        uniqueHitsSoFar = 0;
        hitIds?.Clear();

        if (muzzleTrailPrefab != null)
        {
            spawnedTrail = Instantiate(muzzleTrailPrefab, transform.position, transform.rotation, transform);
        }
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        // Ensure trigger if using trigger logic; leave as collider if you do physics-based hits
        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;

        hitIds = new HashSet<int>(8);
    }

    private void OnEnable()
    {
        // Safety for pooled objects
        deathAt = Time.time + lifetime;
        uniqueHitsSoFar = 0;
        hitIds.Clear();
    }

    private void OnDisable()
    {
        // Clean any trail we may have spawned
        if (spawnedTrail != null)
        {
            Destroy(spawnedTrail);
            spawnedTrail = null;
        }
        // Clear references to avoid memory leaks across pools
        ownerRoot = null;
        ownerCharge = null;
    }

    private void Update()
    {
        if (Time.time >= deathAt)
            Despawn();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Ignore self/owner
        if (ownerRoot != null && other.transform.root == ownerRoot) return;

        var otherLayer = 1 << other.gameObject.layer;

        // If we touched a non-hittable obstruction (e.g., walls), die immediately
        if (dieOnObstruction && (obstructionMask.value & otherLayer) != 0)
        {
            SpawnHitVfx(other.ClosestPoint(transform.position));
            Despawn();
            return;
        }

        // Not a hittable target? Ignore.
        if ((hitMask.value & otherLayer) == 0) return;

        // Try to get a Target (your enemies derive from Target in your project)
        var target = other.GetComponentInParent<Target>();
        if (target == null) return; // Not something we count as an enemy

        // Make sure we only count this collider/root once
        var id = other.transform.root.GetInstanceID();
        if (!hitIds.Add(id)) return;

        // At this point it's a genuine, first-time hit on a valid target.
        // 1) Hand off to your existing damage system (if you like—kept optional):
        // target.TakeDamage?(...);  <-- keep this commented unless you want damage here.
        // 2) Increment special charge:
        ownerCharge?.AddHits(1);

        uniqueHitsSoFar++;
        SpawnHitVfx(other.ClosestPoint(transform.position));

        if (uniqueHitsSoFar >= Mathf.Max(1, maxUniqueHits))
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
        // If you later switch to pooling, replace with pool.Release(this)
        Destroy(gameObject);
    }

    #region Designer Helpers (optional setters)
    public void SetSpeed(float s) => speed = s;
    public void SetLifetime(float t) => lifetime = t;
    public void SetPierce(int n) => maxUniqueHits = Mathf.Max(1, n);
    public void SetHitMask(LayerMask m) => hitMask = m;
    public void SetObstructionMask(LayerMask m) => obstructionMask = m;
    #endregion
}
