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
        if (Time.time - lastHitTime < hitCooldown)
            return;

        // Ignore self
        if (owner != null && other.transform.root.gameObject == owner)
            return;

        // Check layer mask — if other object's layer isn't in the mask, skip
        if ((targetLayers.value & (1 << other.gameObject.layer)) == 0)
            return;

        // Try to damage
        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damage);
            lastHitTime = Time.time;

            if (destroyOnHit)
            {
                // Prefer pooled release if available
                if (TryGetComponent<IPooledRelease>(out var pooled))
                    pooled.Release();
                else
                    Destroy(gameObject);
            }
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
}