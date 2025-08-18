using System;
using System.Collections.Generic;

public static class Pause
{
    static readonly HashSet<object> owners = new();
    public static bool IsPaused => owners.Count > 0;
    public static event Action<bool> OnChanged;

    public static void Request(object owner)
    {
        owner ??= typeof(Pause);
        if (owners.Add(owner)) OnChanged?.Invoke(true);
    }

    public static void Release(object owner)
    {
        owner ??= typeof(Pause);
        if (owners.Remove(owner) && owners.Count == 0) OnChanged?.Invoke(false);
    }

    public static void Set(bool paused, object owner)
    {
        if (paused) Request(owner);
        else Release(owner);
    }
}