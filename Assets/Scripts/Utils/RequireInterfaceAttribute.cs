using System;
using UnityEngine;

/// Attribute to force a field to accept only components that implement a given interface.
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class RequireInterfaceAttribute : PropertyAttribute
{
    public Type RequiredType { get; private set; }

    public RequireInterfaceAttribute(Type requiredType)
    {
        RequiredType = requiredType;
    }
}