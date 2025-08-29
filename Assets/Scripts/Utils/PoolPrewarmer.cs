using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Scene helper that pre-fills pools for hot prefabs.
/// Drop this in your bootstrap scene, assign prefabs and counts, and hit Play.
/// </summary>
public class PoolPrewarmer : MonoBehaviour
{
    [System.Serializable]
    public struct PoolPrewarmEntry
    {
        public GameObject prefab;
        [Min(0)] public int count;
    }

    [System.Serializable]
    public struct WeaponPrewarm
    {
        public WeaponDefinition definition;
        [Min(0)] public int projectile;
        [Min(0)] public int muzzleFlash;
        // Note: WeaponDefinition has no hitVFXPrefab field. If you add one later,
        // you can extend this struct again, but for now we keep it aligned.
    }

    [Header("Service")]
    [SerializeField] private UnityPoolService pools;

    [Header("Behaviour")]
    [SerializeField] private bool runOnStart = true;

    [Header("Direct Prefabs")]
    [Tooltip("Any prefab you intend to spawn repeatedly (bullets, pellets, impact FX, debris, casing ejections, etc.)")]
    [SerializeField] private List<PoolPrewarmEntry> entries = new();

    [Header("From Weapon Definitions (optional)")]
    [SerializeField] private List<WeaponPrewarm> weaponEntries = new();

    void Awake()
    {
        if (!pools) pools = FindFirstObjectByType<UnityPoolService>();
    }

    void Start()
    {
        if (runOnStart) PrewarmAll();
    }

    /// <summary>
    /// Aggregates duplicate prefabs and prewarms each exactly once.
    /// Safe to call multiple times; subsequent calls will add to the pool capacity.
    /// </summary>
    public void PrewarmAll()
    {
        if (!pools)
        {
            Debug.LogWarning("[PoolPrewarmer] No UnityPoolService found in scene.");
            return;
        }

        var totals = new Dictionary<GameObject, int>();

        // Collect from direct entries
        foreach (var e in entries)
        {
            if (!e.prefab || e.count <= 0) continue;
            totals.TryGetValue(e.prefab, out var current);
            totals[e.prefab] = current + e.count;
        }

        // Collect from weapon definitions
        foreach (var w in weaponEntries)
        {
            if (!w.definition) continue;

            if (w.projectile > 0 && w.definition.projectilePrefab)
            {
                var p = w.definition.projectilePrefab;
                totals.TryGetValue(p, out var c); totals[p] = c + w.projectile;
            }

            if (w.muzzleFlash > 0 && w.definition.muzzleFlashPrefab)
            {
                var p = w.definition.muzzleFlashPrefab;
                totals.TryGetValue(p, out var c); totals[p] = c + w.muzzleFlash;
            }
        }

        // Execute prewarm
        foreach (var kvp in totals)
        {
            var prefab = kvp.Key;
            int count = kvp.Value;
            if (!prefab || count <= 0) continue;
            pools.Prewarm(prefab, count);
        }

        Debug.Log($"[PoolPrewarmer] Prewarmed {totals.Count} prefab(s).");
    }
}
