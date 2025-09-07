using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lightweight global registry of live Enemy instances.
/// No polling, no scene scans, safe across domain reloads.
/// </summary>
public static class EnemyRegistry
{
    private static readonly HashSet<Enemy> all = new HashSet<Enemy>();

    public static IReadOnlyCollection<Enemy> All => all;
    public static event Action<Enemy> EnemyAdded;
    public static event Action<Enemy> EnemyRemoved;

    internal static void Add(Enemy e)
    {
        if (e && all.Add(e)) EnemyAdded?.Invoke(e);
    }

    internal static void Remove(Enemy e)
    {
        if (e && all.Remove(e)) EnemyRemoved?.Invoke(e);
    }

    // Clear statics between play sessions / scene loads
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        all.Clear();
        EnemyAdded = null;
        EnemyRemoved = null;
    }
}