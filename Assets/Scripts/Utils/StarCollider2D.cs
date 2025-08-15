using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[AddComponentMenu("Physics 2D/Star Collider 2D")]
[RequireComponent(typeof(PolygonCollider2D))]
public class StarCollider2D : MonoBehaviour
{
    [Header("Star geometry")]
    [Min(0.0001f)] public float radiusA = 1f;   // inner radius
    [Min(0.0001f)] public float radiusB = 2f;   // outer radius
    [Range(3, 72)] public int points = 5;       // number of star tips
    [Range(0f, 360f)] public float rotation = 0f; // degrees, counter-clockwise

    [Tooltip("Optional local offset for the star centre, in the collider's local space.")]
    public Vector2 localOffset = Vector2.zero;

    PolygonCollider2D poly;

    void Awake()
    {
        Cache();
        RebuildStar();
    }

    void Reset()
    {
        Cache();
        RebuildStar();
    }

    void OnEnable()
    {
        Cache();
        RebuildStar();
    }

    void OnValidate()
    {
        // Called in editor when values change; keep collider up to date.
        Cache();
        RebuildStar();
    }

    void Cache()
    {
        if (poly == null) poly = GetComponent<PolygonCollider2D>();
    }

    /// <summary>
    /// Regenerates the polygon collider path to a star shape.
    /// </summary>
    public void RebuildStar()
    {
        if (poly == null) return;

        int p = Mathf.Max(3, points);                   // at least a triangle star
        int vertCount = p * 2;                           // alternating inner/outer
        var pts = new Vector2[vertCount];

        float ang = rotation * Mathf.Deg2Rad;
        float step = (Mathf.PI * 2f) / vertCount;

        for (int i = 0; i < vertCount; i++)
        {
            float r = (i % 2 == 0) ? radiusA : radiusB;  // inner, outer, inner, ...
            float x = localOffset.x + r * Mathf.Cos(ang);
            float y = localOffset.y + r * Mathf.Sin(ang);
            pts[i] = new Vector2(x, y);
            ang += step;
        }

        // Assign a single closed path (PolygonCollider2D closes it implicitly; do not repeat first point)
        poly.pathCount = 1;
        poly.SetPath(0, pts);
        // Optional: ensure no auto-generation interferes
        poly.autoTiling = false;
    }

    /// <summary>
    /// Returns a copy of the current star points (local space), in winding order.
    /// </summary>
    public Vector2[] GetPoints()
    {
        int p = Mathf.Max(3, points);
        int vertCount = p * 2;
        var list = new List<Vector2>(vertCount);

        float ang = rotation * Mathf.Deg2Rad;
        float step = (Mathf.PI * 2f) / vertCount;

        for (int i = 0; i < vertCount; i++)
        {
            float r = (i % 2 == 0) ? radiusA : radiusB;
            list.Add(new Vector2(
                localOffset.x + r * Mathf.Cos(ang),
                localOffset.y + r * Mathf.Sin(ang)));
            ang += step;
        }
        return list.ToArray();
    }

#if UNITY_EDITOR
    // Nice visual in the editor
    void OnDrawGizmosSelected()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        var pts = GetPoints();
        for (int i = 0; i < pts.Length; i++)
        {
            var a = pts[i];
            var b = pts[(i + 1) % pts.Length];
            Gizmos.DrawLine(a, b);
        }
    }
#endif
}