using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

[RequireComponent(typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour
{
    [Header("Motion")]
    [SerializeField] private float speed = 18f;
    [SerializeField] private float lifetime = 5f;

    [Header("Hit Settings")]
    [SerializeField] private LayerMask targetLayers;    // who we can damage
    [SerializeField] private LayerMask obstacleLayers;  // walls/environment that stop the shot
    [SerializeField] private float radius = 0.06f;      // >0 uses CircleCast (safer than thin ray)
    [SerializeField] private float skin = 0.01f;        // back off from the surface slightly
    [SerializeField] private float damage = 5f;
    [SerializeField] private int maxPierces = 0;        // 0 = stop on first target; 2 = pierce two targets, etc.
    [SerializeField] private bool pierceThroughObstacles = false; // if true, walls don't stop (rare)

    [Header("FX (optional)")]
    [SerializeField] private GameObject hitVFX;

    // Pool hook (set by spawner)
    public IObjectPool<Projectile> ObjectPool { get; set; }

    private Rigidbody2D rb;
    private Transform ownerRoot;
    private Vector2 dir;
    private float despawnAt;
    private readonly HashSet<Collider2D> hitThisStep = new HashSet<Collider2D>();

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    /// <summary>Called by spawner immediately after Instantiate/Get.</summary>
    public void Init(GameObject owner, LayerMask targets, float dmg, Vector2 direction)
    {
        ownerRoot = owner ? owner.transform.root : null;
        targetLayers = targets;
        damage = dmg;
        dir = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.right;
        despawnAt = Time.time + lifetime;
    }

    private void OnEnable()
    {
        // in case pooled object was reused
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
        bool blocked = false;
        Vector2 lastImpactPoint = start;

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
                    lastImpactPoint = hit.point;

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
                blocked = true;
                lastImpactPoint = hit.point;
                MoveToImpact(start, hit.distance);
                Release();
                return;
            }
        }

        // If we pierced up to allowance and weren't blocked by a wall, keep going
        if (!blocked)
        {
            rb.MovePosition(start + step);
        }
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
        // Return to pool if possible; otherwise destroy
        if (ObjectPool != null) ObjectPool.Release(this);
        else Destroy(gameObject);
    }
}