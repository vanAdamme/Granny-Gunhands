using System;
using System.Collections.Generic;
using UnityEngine;

public class VFXAttachPoints : MonoBehaviour
{
    [Serializable] public class NamedAnchor { public string name; public Transform anchor; }

    [Tooltip("Optional: declare named attach points for VFX (e.g., \"Head\", \"Chest\", \"Feet\").")]
    public List<NamedAnchor> anchors = new();

    /// <summary>Find an anchor by name; returns null if not found.</summary>
    public Transform Get(string anchorName)
    {
        if (string.IsNullOrEmpty(anchorName)) return null;
        for (int i = 0; i < anchors.Count; i++)
        {
            var a = anchors[i];
            if (a != null && a.name == anchorName) return a.anchor;
        }
        return null;
    }
}