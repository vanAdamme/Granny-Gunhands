using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Damager : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private float damage = 1f;
    [SerializeField] private bool destroyOnHit = false;
    [SerializeField] private float hitCooldown = 0.1f;

    [Tooltip("Layers that this hitbox can damage")]
    [SerializeField] private LayerMask targetLayers;

    private GameObject owner;
    private float lastHitTime = -999f;

    private void Awake()
    {
        // Ensure trigger is set so OnTriggerEnter2D works
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Hit(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // For contact damage — allow re-hitting after cooldown
        if (hitCooldown > 0f)
            Hit(other);
    }
    
    private void Hit(Collider2D other)
    {
        if (Time.time - lastHitTime < hitCooldown) return;

        // Ignore self/owner
        if (owner != null && other.transform.root.gameObject == owner) return;

        // Ignore anything under the same spawned projectile root
        if (other.transform.root == transform.root) return;

        // Use the RB host or root as the authoritative “team layer”
        GameObject targetGO = other.attachedRigidbody
            ? other.attachedRigidbody.gameObject
            : other.transform.root.gameObject;

        if (!IsInLayerMask(targetGO, targetLayers)) return;

        var damageable = other.GetComponentInParent<IDamageable>();
        if (damageable == null) return;

        damageable.TakeDamage(damage);
        lastHitTime = Time.time;

        if (destroyOnHit)
        {
            // Prefer pool release if available
            var pooled = GetComponent<IPooledRelease>();
            if (pooled != null)
                pooled.Release();
            else
                Destroy(gameObject);
        }
    }

    /// <summary>
    /// Called by the spawner (enemy, player, projectile) to set who owns this hitbox and what it can hit.
    /// </summary>
    public void Configure(GameObject ownerObj, LayerMask targets, float dmg)
    {
        owner = ownerObj;
        targetLayers = targets;
        damage = dmg;
    }

    private static bool IsInLayerMask(GameObject go, LayerMask mask)
    {
        return (mask.value & (1 << go.layer)) != 0;
    }
}