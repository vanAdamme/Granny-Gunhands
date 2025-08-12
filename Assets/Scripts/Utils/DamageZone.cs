using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Collider2D))]
public class DamageZone : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private float damagePerTick = 1f;
    [SerializeField, Min(0f)] private float tickInterval = 0.2f;

    [Header("Targeting")]
    [SerializeField] private LayerMask targetLayers;
    [SerializeField] private GameObject owner; // optional; ignored if null

    private Collider2D col;
    private readonly ContactFilter2D filter = new ContactFilter2D() { useTriggers = true };
    private readonly List<Collider2D> overlaps = new List<Collider2D>(16);
    private readonly Dictionary<int, float> nextTickAt = new Dictionary<int, float>();

    void Awake()
    {
        col = GetComponent<Collider2D>();
        col.isTrigger = true; // zone should be a trigger
    }

    void FixedUpdate()
    {
        overlaps.Clear();
        col.Overlap(filter, overlaps); // ask physics for current overlaps (stable, once per step)

        // Deduplicate multiple colliders from the same target in this step
        var processedThisStep = new HashSet<int>();

        float now = Time.time;

        foreach (var c in overlaps)
        {
            if (!c) continue;

            // Resolve target root
            var root = c.attachedRigidbody ? c.attachedRigidbody.transform.root : c.transform.root;
            if (!root) continue;

            if (owner && root.gameObject == owner) continue;                // ignore owner
            if ((targetLayers.value & (1 << root.gameObject.layer)) == 0) continue; // layer mask

            // Must have something we can damage
            var dmgComp = root.GetComponentInChildren<IDamageable>() as Component;
            if (!dmgComp) continue;

            int id = root.GetInstanceID();
            if (!processedThisStep.Add(id)) continue; // already handled this target this step

            // Respect i-frames without burning the timer
            var health = root.GetComponentInChildren<Health>();
            if (health && health.IsInvulnerable) continue;

            // Tick gate
            if (nextTickAt.TryGetValue(id, out float next) && now < next) continue;

            // Apply damage and schedule next tick
            (dmgComp as IDamageable).TakeDamage(damagePerTick);
            nextTickAt[id] = now + tickInterval;
        }

        // Optional housekeeping: prune long-gone entries (keeps dict small even if targets despawn while inside)
        // Not strictly required, but nice to have:
        // You can add a small periodic sweep here if you like.
    }

    /// <summary>Optional: configure at runtime.</summary>
    public void Configure(GameObject ownerObj, LayerMask targets, float dmgPerTick, float interval)
    {
        owner = ownerObj;
        targetLayers = targets;
        damagePerTick = dmgPerTick;
        tickInterval = Mathf.Max(0f, interval);
    }
}