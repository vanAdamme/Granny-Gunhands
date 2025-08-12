using UnityEngine;
using System.Collections.Generic;

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
    private readonly Dictionary<Collider2D, float> _nextAllowedHit = new();

    private void Awake()
    {
        // Ensure trigger is set so OnTriggerEnter2D works
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerStay2D(Collider2D other) => Hit(other);

    private void OnTriggerExit2D(Collider2D other)
    {
        _nextAllowedHit.Remove(other);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Hit(other);
    }

    private void Hit(Collider2D other)
    {
        // ignore owner / same root
        if (owner && other.transform.root.gameObject == owner) return;
        if (other.transform.root == transform.root) return;

        // layer check
        GameObject targetGO = other.attachedRigidbody ? other.attachedRigidbody.gameObject : other.transform.root.gameObject;
        if ((targetLayers.value & (1 << targetGO.layer)) == 0) return;

        // per-target cooldown gate
        float now = Time.time;
        if (_nextAllowedHit.TryGetValue(other, out float next) && now < next) return;

        // don’t waste cooldown while invulnerable
        var health = other.GetComponentInParent<Health>();
        if (health != null && health.IsInvulnerable) return;

        // apply damage
        var damageable = other.GetComponentInParent<IDamageable>();
        if (damageable == null) return;

        damageable.TakeDamage(damage);

        // start cooldown for THIS collider only
        _nextAllowedHit[other] = now + hitCooldown;

        if (destroyOnHit)
        {
            if (TryGetComponent<IPooledRelease>(out var pooled)) pooled.Release();
            else Destroy(gameObject);
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