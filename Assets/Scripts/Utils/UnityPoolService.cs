using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-1000)]
public class UnityPoolService : MonoBehaviour, IGameObjectPool
{
    [Header("Pool Settings")]
    [SerializeField] private bool collectionChecks = false;
    [SerializeField] private int defaultCapacity = 16;
    [SerializeField] private int maxSize = 256;

    [Header("Hierarchy Hygiene")]
    [Tooltip("Optional parent for ACTIVE instances (e.g., a 'Projectiles' GameObject). Leave empty to keep active items unparented.")]
    [SerializeField] private Transform activeParent;
    [Tooltip("If true, even ACTIVE instances are hidden in the Hierarchy.")]
    [SerializeField] private bool hideActiveInHierarchy = false;

    private readonly Dictionary<GameObject, IObjectPool<GameObject>> pools = new();
    private readonly Dictionary<GameObject, GameObject> instanceToPrefab = new();

    // Hidden container for INACTIVE instances
    private Transform hiddenRoot;

    void OnEnable()
    {
        EnsureHiddenRoot();
        SceneManager.activeSceneChanged += OnSceneChanged;
    }

    void OnDisable()
    {
        SceneManager.activeSceneChanged -= OnSceneChanged;

        // Safety: destroy any pooled objects on disable
        foreach (var p in pools.Values) p.Clear();
        pools.Clear();
        instanceToPrefab.Clear();

        if (hiddenRoot)
        {
            // Destroy the hidden container when the service goes away
            Destroy(hiddenRoot.gameObject);
            hiddenRoot = null;
        }
    }

    void OnSceneChanged(Scene _, Scene __)
    {
        // Scene-local service; flush pools on load to avoid leaking scene objects
        foreach (var p in pools.Values) p.Clear();
        pools.Clear();
        instanceToPrefab.Clear();

        // Recreate a clean hidden root for the new scene
        if (hiddenRoot) Destroy(hiddenRoot.gameObject);
        EnsureHiddenRoot();
    }

    public void Prewarm(GameObject prefab, int count)
    {
        var pool = GetOrCreatePool(prefab);
        var tmp = new List<GameObject>(count);
        for (int i = 0; i < count; i++) tmp.Add(pool.Get());
        for (int i = 0; i < tmp.Count; i++) pool.Release(tmp[i]);
    }

    public GameObject Spawn(GameObject prefab, Vector3 pos, Quaternion rot, Transform parent = null)
    {
        var pool = GetOrCreatePool(prefab);
        var go = pool.Get();
        var t = go.transform;

        // Parent to caller's requested parent, else to configured active parent.
        var targetParent = parent ? parent : activeParent;
        if (t.parent != targetParent) t.SetParent(targetParent, false);

        t.SetPositionAndRotation(pos, rot);

        // Make sure active instances are visible unless overridden
        go.hideFlags = hideActiveInHierarchy ? HideFlags.HideInHierarchy : HideFlags.None;

        return go;
    }

    public void Despawn(GameObject instance)
    {
        if (!instance || !instanceToPrefab.TryGetValue(instance, out var prefab))
        {
            // Fallback: if we lost the mapping, just destroy to avoid leaks.
            if (instance) Destroy(instance);
            return;
        }

        var pool = pools[prefab];
        pool.Release(instance);
    }

    private IObjectPool<GameObject> GetOrCreatePool(GameObject prefab)
    {
        if (pools.TryGetValue(prefab, out var existing)) return existing;

        GameObject Create()
        {
            var go = Instantiate(prefab);

            // Attach a helper so objects can return themselves without knowing the service.
            var po = go.GetComponent<PooledObject>() ?? go.AddComponent<PooledObject>();
            po.Configure(this);

            instanceToPrefab[go] = prefab;

            // Newly created pooled instances start inactive under the hidden root.
            if (hiddenRoot) go.transform.SetParent(hiddenRoot, false);
            go.hideFlags = HideFlags.HideInHierarchy;
            go.SetActive(false);

            return go;
        }

        void OnGet(GameObject go)
        {
            // Activate and show (unless you've opted to hide active too)
            go.SetActive(true);
            go.hideFlags = hideActiveInHierarchy ? HideFlags.HideInHierarchy : HideFlags.None;

            // Give listeners a chance to initialize
            go.SendMessage("OnSpawnedFromPool", SendMessageOptions.DontRequireReceiver);
        }

        void OnRelease(GameObject go)
        {
            // Let listeners clean up before hiding
            go.SendMessage("OnDespawnedToPool", SendMessageOptions.DontRequireReceiver);

            // Park under hidden root & hide in hierarchy while inactive
            if (hiddenRoot && go.transform.parent != hiddenRoot)
                go.transform.SetParent(hiddenRoot, false);

            go.hideFlags = HideFlags.HideInHierarchy;
            go.SetActive(false);
        }

        void OnDestroy(GameObject go)
        {
            if (go) Destroy(go);
        }

        var pool = new ObjectPool<GameObject>(
            Create, OnGet, OnRelease, OnDestroy,
            collectionChecks, defaultCapacity, maxSize);

        pools[prefab] = pool;
        return pool;
    }

    private void EnsureHiddenRoot()
    {
        if (hiddenRoot) return;

        var go = new GameObject("Pool_Inactive");
        // Keep it out of the way in the Hierarchy
        go.hideFlags = HideFlags.HideInHierarchy;
        hiddenRoot = go.transform;
        // Scene-local service: do NOT DontDestroyOnLoad here (we flush on scene change)
    }
}
