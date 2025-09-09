using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PolygonCollider2D))]
public class CopyCompositeToPolygon : MonoBehaviour
{
    [Tooltip("Composite on FloorBoundary (same room). If null, will search upwards.")]
    public CompositeCollider2D sourceComposite;

    void Awake()
    {
        if (!sourceComposite)
            sourceComposite = GetComponentInParent<CompositeCollider2D>();

        var poly = GetComponent<PolygonCollider2D>();
        if (!sourceComposite || !poly) return;

        int pathCount = sourceComposite.pathCount;
        poly.pathCount = pathCount;

        var buffer = new List<Vector2>(256);
        for (int i = 0; i < pathCount; i++)
        {
            buffer.Clear();
            buffer.Capacity = Mathf.Max(buffer.Capacity, sourceComposite.GetPathPointCount(i));
            sourceComposite.GetPath(i, buffer);
            poly.SetPath(i, buffer.ToArray());
        }
        poly.isTrigger = true;
    }
}