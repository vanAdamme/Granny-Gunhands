using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-1000)]
public class UnityPoolService : MonoBehaviour, IGameObjectPool
{
    [SerializeField] private bool collectionChecks = false;
    [SerializeField] private int defaultCapacity = 16;
    [SerializeField] private int maxSize = 256;

    private readonly Dictionary<GameObject, IObjectPool<GameObject>> pools = new();
    private readonly Dictionary<GameObject, GameObject> instanceToPrefab = new();

    void OnEnable()
    {
        SceneManager.activeSceneChanged += OnSceneChanged;
    }

    void OnDisable()
    {
        SceneManager.activeSceneChanged -= OnSceneChanged;
        // Safety: destroy any pooled objects on disable
        foreach (var p in pools.Values) p.Clear(); 
        pools.Clear();
        instanceToPrefab.Clear();
    }

    void OnSceneChanged(Scene _, Scene __)
    {
        // Scene-local service; flush pools on load to avoid leaking scene objects
        foreach (var p in pools.Values) p.Clear();
        pools.Clear();
        instanceToPrefab.Clear();
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
        if (parent) t.SetParent(parent, false);
        t.SetPositionAndRotation(pos, rot);
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
            go.SetActive(false);
            return go;
        }

        void OnGet(GameObject go)   { go.SetActive(true);   go.SendMessage("OnSpawnedFromPool", SendMessageOptions.DontRequireReceiver); }
        void OnRelease(GameObject go){ go.SendMessage("OnDespawnedToPool", SendMessageOptions.DontRequireReceiver); go.SetActive(false); }
        void OnDestroy(GameObject go){ if (go) Destroy(go); }

        var pool = new ObjectPool<GameObject>(Create, OnGet, OnRelease, OnDestroy, collectionChecks, defaultCapacity, maxSize);
        pools[prefab] = pool;
        return pool;
    }
}