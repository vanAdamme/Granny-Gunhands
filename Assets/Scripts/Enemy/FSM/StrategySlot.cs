using System;
using UnityEngine;

[Serializable]
public struct StrategySlot
{
    [SerializeField] private string _assemblyQualifiedName;   // stored type
    [SerializeField] private string _niceName;                // cached label

    public string AssemblyQualifiedName => _assemblyQualifiedName;
    public string NiceName => string.IsNullOrEmpty(_niceName) ? "(None)" : _niceName;

    public Type ResolveType()
    {
        if (string.IsNullOrEmpty(_assemblyQualifiedName)) return null;
        return Type.GetType(_assemblyQualifiedName, throwOnError: false);
    }

    public void SetType(Type t)
    {
        _assemblyQualifiedName = t != null ? t.AssemblyQualifiedName : null;
        _niceName = t != null ? t.Name : "(None)";
    }
}