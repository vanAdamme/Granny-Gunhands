using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public class GrenadeProjectile : MonoBehaviour
{
    [Header("Explosion")]
    [SerializeField] private float fuseSeconds = 1.2f;
    [SerializeField] private float explosionRadius = 1.6f;
    [SerializeField] private GameObject explosionVFX;

    // Pre-allocated to avoid per-explosion heap allocation (non-allocating physics API, Unity 6.3+).
    private static readonly Collider2D[] OverlapBuffer = new Collider2D[32];

    private Rigidbody2D rb;
    private float despawnAt;
    private float damage;
    private LayerMask targetLayers;
    private GameObject ownerRoot;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f; // top-down
        rb.linearDamping = 0.8f;
    }

    public void Launch(GameObject owner, LayerMask targets, float dmg, Vector2 dir, float speed, float range)
    {
        ownerRoot = owner ? owner.transform.root.gameObject : null;
        targetLayers = targets;
        damage = dmg;

        rb.linearVelocity = dir.normalized * speed;
        despawnAt = Time.time + Mathf.Max(0.3f, fuseSeconds);
    }

    void Update()
    {
        if (Time.time >= despawnAt)
            Explode();
    }

    void OnCollisionEnter2D(Collision2D _)
    {
        // Optional: stick, bounce, or reduce speed. For now: explode immediately on contact.
        Explode();
    }

    private void Explode()
    {
        // FX
        if (explosionVFX)
            VFX.Spawn(explosionVFX, transform.position, Quaternion.identity, 1.2f);

        // Damage in radius — ContactFilter2D overload is the non-deprecated non-allocating path in Unity 6.3+.
        // useTriggers = true matches the old OverlapCircleAll/NonAlloc behaviour (includes trigger colliders).
        var filter = new ContactFilter2D { useLayerMask = true, layerMask = targetLayers, useTriggers = true };
        int hitCount = Physics2D.OverlapCircle(transform.position, explosionRadius, filter, OverlapBuffer);
        for (int i = 0; i < hitCount; i++)
        {
            var h = OverlapBuffer[i];
            if (!h) continue;
            if (ownerRoot && h.transform.root.gameObject == ownerRoot) continue;

            var d = h.GetComponentInParent<IDamageable>();
            if (d != null) d.TakeDamage(damage);
        }

        Destroy(gameObject);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
#endif
}