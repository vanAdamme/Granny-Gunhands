using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

[RequireComponent(typeof(CircleCollider2D))]
public class CookieBribeDecoy : MonoBehaviour
{
    [Header("Lifetime & Aura")]
    [SerializeField] private float lifetime = 6f;
    [SerializeField] private float auraRadius = 6f;
    [SerializeField] private LayerMask enemyLayers = ~0;

    [Header("Repathing")]
    [Tooltip("Occasionally force a path search so A* reacts quickly to the moving cookie.")]
    [SerializeField] private float searchInterval = 0.35f;

    [Header("VFX (optional)")]
    [SerializeField] private GameObject spawnVFX;
    [SerializeField] private GameObject auraVFX;
    [SerializeField] private GameObject endVFX;

    private readonly HashSet<AIPath> influenced = new HashSet<AIPath>();
    private float despawnAt;
    private float nextSearchAt;

    void Awake()
    {
        var col = GetComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = auraRadius;

        if (spawnVFX) VFX.Spawn(spawnVFX, transform.position, Quaternion.identity, 1.5f);
        if (auraVFX)  VFX.SpawnAttached(auraVFX, transform, transform.position, 1.5f, autoDestroy:false);

        despawnAt = Time.time + lifetime;
        nextSearchAt = 0f;
    }

    void Update()
    {
        if (Time.time >= despawnAt)
        {
            if (endVFX) VFX.Spawn(endVFX, transform.position, Quaternion.identity, 1.2f);
            Destroy(gameObject);
        }
    }

    // IMPORTANT: write after Enemy.Update() so the cookie wins the target race
    void LateUpdate()
    {
        if (influenced.Count == 0) return;

        bool doSearch = Time.time >= nextSearchAt;
        if (doSearch) nextSearchAt = Time.time + searchInterval;

        // Clean up dead refs while we go
        var toRemove = ListPool<AIPath>.Claim();   // or use a small local list if you don't have a pool util

        foreach (var ai in influenced)
        {
            if (!ai || !ai.isActiveAndEnabled)
            {
                toRemove.Add(ai);
                continue;
            }

            ai.destination = transform.position;
            if (doSearch && !ai.pathPending) ai.SearchPath();
        }

        // remove null/disabled entries
        for (int i = 0; i < toRemove.Count; i++) influenced.Remove(toRemove[i]);
        ListPool<AIPath>.Release(toRemove);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        TryAdd(other);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        // Cheap re-add is fine; HashSet ignores duplicates
        TryAdd(other);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        var root = other.attachedRigidbody ? other.attachedRigidbody.transform.root : other.transform.root;
        if (!root) return;

        if (!IsOnEnemyLayer(root.gameObject)) return;

        var ai = root.GetComponent<AIPath>() ?? root.GetComponentInChildren<AIPath>(true);
        if (ai) influenced.Remove(ai);
    }

    private void TryAdd(Collider2D other)
    {
        var root = other.attachedRigidbody ? other.attachedRigidbody.transform.root : other.transform.root;
        if (!root) return;

        if (!IsOnEnemyLayer(root.gameObject)) return;
        if (!root.GetComponent<Enemy>()) return;   // only influence real enemies

        var ai = root.GetComponent<AIPath>() ?? root.GetComponentInChildren<AIPath>(true);
        if (ai) influenced.Add(ai);
    }

    private bool IsOnEnemyLayer(GameObject go) => (enemyLayers.value & (1 << go.layer)) != 0;

    public void SetLifetime(float seconds)
    {
        lifetime = Mathf.Max(0.05f, seconds);
        despawnAt = Time.time + lifetime;   // refresh the countdown even if Awake already ran
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, auraRadius);
    }
#endif
}

// Minimal, no-alloc helper; swap to your pooling util or remove if undesired.
static class ListPool<T>
{
    static readonly Stack<List<T>> pool = new Stack<List<T>>();
    public static List<T> Claim() => pool.Count > 0 ? pool.Pop() : new List<T>();
    public static void Release(List<T> list) { list.Clear(); pool.Push(list); }
}