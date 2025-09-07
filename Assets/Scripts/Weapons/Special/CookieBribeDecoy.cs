using System.Collections.Generic;
using UnityEngine;

public class CookieBribeDecoy : MonoBehaviour
{
    [Header("Lifetime & Aura")]
    [SerializeField] private float lifetime    = 6f;
    [SerializeField] private float auraRadius  = 8f;
    [SerializeField] private float refreshRate = 0.25f; // how often we recompute who’s in range

    [Header("Optional VFX")]
    [SerializeField] private GameObject spawnVFX;
    [SerializeField] private GameObject endVFX;

    private readonly HashSet<Enemy> influenced = new HashSet<Enemy>();
    private float despawnAt;
    private float nextRefreshAt;

    void Awake()
    {
        if (spawnVFX) VFX.Spawn(spawnVFX, transform.position, Quaternion.identity, 1.0f);
        despawnAt     = Time.time + lifetime;
        nextRefreshAt = 0f;
    }

    void Update()
    {
        if (Time.time >= despawnAt)
        {
            if (endVFX) VFX.Spawn(endVFX, transform.position, Quaternion.identity, 1.0f);
            ReleaseAll();
            Destroy(gameObject);
            return;
        }

        if (Time.time >= nextRefreshAt)
        {
            nextRefreshAt = Time.time + refreshRate;
            RefreshInfluence();
        }
    }

    void OnDisable() => ReleaseAll();

    private void RefreshInfluence()
    {
        // Build the current set of enemies within the aura.
        var current = new HashSet<Enemy>();
        float r2 = auraRadius * auraRadius;
        Vector3 c = transform.position;

        // Iterate the registry (O(N_enemies), no allocations apart from this small set)
        foreach (var e in EnemyRegistry.All)
        {
            if (!e || !e.isActiveAndEnabled) continue;

            // distance filter
            if ((e.transform.position - c).sqrMagnitude > r2) continue;

            current.Add(e);

            // Newly influenced? Point them at the cookie.
            if (!influenced.Contains(e))
                e.SetTargetOverride(transform, this);
        }

        // Remove those that left the aura
        foreach (var e in influenced)
        {
            if (!current.Contains(e))
                e.ClearTargetOverride(this);
        }

        // Swap sets (keep our tracking in sync)
        influenced.Clear();
        foreach (var e in current) influenced.Add(e);
    }

    void ReleaseAll()
    {
        foreach (var e in influenced)
            if (e) e.ClearTargetOverride(this);

        influenced.Clear();
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, auraRadius);
    }
#endif
}