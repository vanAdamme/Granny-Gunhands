using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Collider2D))]
public class Damager : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private float damage = 1f;
    [SerializeField] private bool destroyOnHit = false;
    [SerializeField, Min(0f)] private float hitCooldown = 0.1f;

    [Tooltip("Layers that this hitbox can damage")]
    [SerializeField] private LayerMask targetLayers;

    private GameObject owner;

    // Per-target cooldown: when we’re next allowed to damage this target
    private readonly Dictionary<int, float> nextAllowedByTarget = new();

    // Per-physics-step guard: prevents multiple hits on the same target within the same FixedUpdate tick
    private readonly Dictionary<int, float> lastHitFixedTimeByTarget = new();

    private void Awake()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other) => TryHit(other);
    private void OnTriggerStay2D(Collider2D other)  => TryHit(other);

    private void OnTriggerExit2D(Collider2D other)
    {
        var dmg = other.GetComponentInParent<IDamageable>() as Component;
        if (!dmg) return;

        int targetId = dmg.transform.root.GetInstanceID();
        nextAllowedByTarget.Remove(targetId);
        lastHitFixedTimeByTarget.Remove(targetId);
    }

    private void TryHit(Collider2D other)
    {
        // Ignore self/same root
        if (owner && other.transform.root.gameObject == owner) return;
        if (other.transform.root == transform.root) return;

        // Layer gate (use rigidbody GO if present, else the root GO)
        GameObject layerGO = other.attachedRigidbody ? other.attachedRigidbody.gameObject : other.transform.root.gameObject;
        if ((targetLayers.value & (1 << layerGO.layer)) == 0) return;

        // Find the damageable target on the root
        var targetComp = other.GetComponentInParent<IDamageable>() as Component;
        if (!targetComp) return;

        int targetId = targetComp.transform.root.GetInstanceID();

        // Respect i-frames without starting cooldown
        var health = targetComp.GetComponent<Health>();
        if (health && health.IsInvulnerable) return;

        // Guard: only one hit per target **per physics step**
        if (lastHitFixedTimeByTarget.TryGetValue(targetId, out float lastFixedTime)
            && Mathf.Approximately(lastFixedTime, Time.fixedTime))
            return;

        // Per-target cooldown
        float now = Time.time;
        if (nextAllowedByTarget.TryGetValue(targetId, out float next) && now < next)
            return;

        // Apply damage
        (targetComp as IDamageable).TakeDamage(damage);

        // Mark hit for this physics step and set cooldown window
        lastHitFixedTimeByTarget[targetId] = Time.fixedTime;
        if (hitCooldown > 0f) nextAllowedByTarget[targetId] = now + hitCooldown;
        else nextAllowedByTarget.Remove(targetId);

        // Optionally destroy/release the damager (e.g., projectile)
        if (destroyOnHit)
        {
            if (TryGetComponent<IPooledRelease>(out var pooled)) pooled.Release();
            else Destroy(gameObject);
        }
    }

    /// <summary>Called by the spawner to set owner and target mask.</summary>
    public void Configure(GameObject ownerObj, LayerMask targets, float dmg)
    {
        owner = ownerObj;
        targetLayers = targets;
        damage = dmg;
    }
}