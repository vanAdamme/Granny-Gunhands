using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public class GrenadeProjectile : MonoBehaviour
{
    [Header("Explosion")]
    [SerializeField] private float fuseSeconds = 1.2f;
    [SerializeField] private float explosionRadius = 1.6f;
    [SerializeField] private GameObject explosionVFX;

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

        // Damage in radius
        var hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius, targetLayers);
        for (int i = 0; i < hits.Length; i++)
        {
            var h = hits[i];
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