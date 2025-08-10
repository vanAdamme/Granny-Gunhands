using System.Collections.Generic;
using UnityEngine;

public class PowerUpBar : MonoBehaviour
{
    [SerializeField] private PowerUpIcon iconPrefab;

    private PowerUpController controller;
    private readonly List<PowerUpController.ActiveInfo> snapshot = new();
    private readonly Dictionary<int, PowerUpIcon> byId = new(); // instanceId -> icon
    private readonly Stack<PowerUpIcon> pool = new();

    private void Start()
    {
        controller = FindFirstObjectByType<PowerUpController>();
        if (!controller) { gameObject.SetActive(false); return; }
    }

    private void Update()
    {
        controller.BuildSnapshot(snapshot);

        // Mark all as unseen
        var seen = HashSetCache.Get();
        try
        {
            foreach (var info in snapshot)
            {
                seen.Add(info.id);
                if (!byId.TryGetValue(info.id, out var icon))
                {
                    icon = GetIcon();
                    byId[info.id] = icon;
                    icon.SetData(info.definition.icon, info.definition.displayName);
                }
                icon.SetTime(info.remaining);
            }

            // Return any icons that are no longer active
            var toRemove = ListCache<int>.Get();
            try
            {
                foreach (var kv in byId)
                    if (!seen.Contains(kv.Key))
                        toRemove.Add(kv.Key);

                foreach (var id in toRemove)
                {
                    ReturnIcon(byId[id]);
                    byId.Remove(id);
                }
            }
            finally { ListCache<int>.Release(toRemove); }
        }
        finally { HashSetCache.Release(seen); }
    }

    private PowerUpIcon GetIcon()
    {
        var icon = pool.Count > 0 ? pool.Pop() : Instantiate(iconPrefab, transform);
        icon.gameObject.SetActive(true);
        return icon;
    }

    private void ReturnIcon(PowerUpIcon icon)
    {
        icon.gameObject.SetActive(false);
        pool.Push(icon);
    }

    // tiny helpers to avoid GC; you can skip these if you prefer simplicity
    private static class HashSetCache
    {
        private static readonly Stack<HashSet<int>> cache = new();
        public static HashSet<int> Get() => cache.Count > 0 ? cache.Pop() : new HashSet<int>();
        public static void Release(HashSet<int> set) { set.Clear(); cache.Push(set); }
    }
    private static class ListCache<T>
    {
        private static readonly Stack<List<T>> cache = new();
        public static List<T> Get() => cache.Count > 0 ? cache.Pop() : new List<T>();
        public static void Release(List<T> list) { list.Clear(); cache.Push(list); }
    }
}