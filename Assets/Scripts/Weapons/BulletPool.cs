using System.Collections.Generic;
using UnityEngine;

public class BulletPool : MonoBehaviour
{
    [System.Serializable]
    public class BulletPoolEntry
    {
        public GameObject prefab;
        public int initialSize = 20;
    }

    [SerializeField] private List<BulletPoolEntry> bulletTypes;

    private Dictionary<GameObject, Queue<GameObject>> poolDict;

    public static BulletPool Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        poolDict = new Dictionary<GameObject, Queue<GameObject>>();

        foreach (var entry in bulletTypes)
        {
            Queue<GameObject> queue = new Queue<GameObject>();
            for (int i = 0; i < entry.initialSize; i++)
            {
                GameObject obj = Instantiate(entry.prefab);
                obj.SetActive(false);
                queue.Enqueue(obj);
            }
            poolDict[entry.prefab] = queue;
        }
    }

    public GameObject GetBullet(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (!poolDict.ContainsKey(prefab))
        {
            poolDict[prefab] = new Queue<GameObject>();
        }

        Queue<GameObject> pool = poolDict[prefab];
        GameObject bullet = null;

        // Try to get an inactive bullet
        while (pool.Count > 0)
        {
            bullet = pool.Dequeue();
            if (!bullet.activeInHierarchy) break;
            bullet = null;
        }

        // If no inactive bullet, instantiate new one
        if (bullet == null)
        {
            bullet = Instantiate(prefab);
        }

        bullet.transform.position = position;
        bullet.transform.rotation = rotation;
        bullet.SetActive(true);

        return bullet;
    }

public void ReturnBullet(GameObject bullet)
{
    bullet.SetActive(false); // Ensure it's truly deactivated
    foreach (var entry in bulletTypes)
    {
        if (bullet.name.StartsWith(entry.prefab.name))
        {
            poolDict[entry.prefab].Enqueue(bullet);
            return;
        }
    }

    Debug.LogWarning("Returned bullet doesn't match any known prefab: " + bullet.name);
}
}
