#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

public static class StrategyTypeCache
{
    // Your real interface names:
    private const string MovementInterfaceName = "IMovementStrategy";
    private const string AttackInterfaceName   = "IAttackStrategy";
    private const string TargetInterfaceName   = "ITargetProvider";

    private static Type[] _movement, _attack, _target;
    public static Type[] MovementTypes => _movement ??= FindTypesDerivedFromInterface(MovementInterfaceName);
    public static Type[] AttackTypes   => _attack   ??= FindTypesDerivedFromInterface(AttackInterfaceName);
    public static Type[] TargetTypes   => _target   ??= FindTypesDerivedFromInterface(TargetInterfaceName);

    private static Type[] FindTypesDerivedFromInterface(string interfaceName)
    {
        var list = new List<Type>();

        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type iface = null;
            try
            {
                foreach (var t in asm.GetTypes())
                {
                    if (t.IsInterface && t.Name == interfaceName) { iface = t; break; }
                }
            }
            catch { continue; }

            if (iface == null) continue;

            try
            {
                foreach (var t in asm.GetTypes())
                {
                    if (t.IsAbstract || t.IsInterface) continue;
                    if (!iface.IsAssignableFrom(t)) continue;
                    if (!typeof(Component).IsAssignableFrom(t)) continue; // must be a Component to add as MonoBehaviour
                    list.Add(t);
                }
            }
            catch { /* skip problematic assemblies */ }
        }

        list.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
        return list.ToArray();
    }
}
#endif
