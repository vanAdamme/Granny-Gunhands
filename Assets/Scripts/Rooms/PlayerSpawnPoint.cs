using UnityEngine;

[AddComponentMenu("Granny/Spawning/Player Spawn Point")]
public class PlayerSpawnPoint : MonoBehaviour
{
    [Tooltip("Higher = chosen first")]
    public int priority = 0;

    [Tooltip("Optional: Facing direction at spawn (world space). Leave zero to ignore.")]
    public Vector2 facing;

    [Header("Safety Check")]
    [Tooltip("Radius to check for overlaps before using this point.")]
    public float safeRadius = 0.25f;

    [Tooltip("Layers considered blocking (walls, obstacles, enemies, etc.).")]
    public LayerMask blockedLayers;

    /// <summary>Can we spawn here without overlapping something?</summary>
    public bool IsSafe()
    {
        if (safeRadius <= 0f) return true;
        var pos = (Vector2)transform.position;
        return Physics2D.OverlapCircle(pos, safeRadius, blockedLayers) == null;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = IsSafe() ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, Mathf.Max(0.05f, safeRadius));

        if (facing.sqrMagnitude > 0.0001f)
        {
            var dir = (Vector3)facing.normalized * 0.5f;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, transform.position + dir);
            Gizmos.DrawSphere(transform.position + dir, 0.03f);
        }
    }
}