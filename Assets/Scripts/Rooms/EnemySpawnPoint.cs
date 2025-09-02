using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class EnemySpawnPoint : MonoBehaviour
{
    [Header("Enemy")]
    [SerializeField] private Health enemyPrefab; // enemy root has Health
    [SerializeField, Min(1)] private int count = 1;

    [Header("Sampling")]
    [Tooltip("Max random offset from this marker, in world units.")]
    [SerializeField, Min(0f)] private float radius = 0.5f;

    [Tooltip("How many random tries per enemy before falling back to the marker position.")]
    [SerializeField, Min(1)] private int maxAttemptsPerEnemy = 20;

    [Header("Floor Constraint (no colliders required)")]
    [SerializeField] private Tilemap floorTilemap;

    [Tooltip("Check neighbouring cells so large enemies don’t clip walls. 0 = only the cell under the spawn point.")]
    [SerializeField, Min(0)] private int cellClearance = 0;

    // If your Floor tile size isn’t 1x1, we derive it from the tilemap.
    private Vector3 cellSize = Vector3.one;

    private void Awake()
    {
        if (!floorTilemap)
            floorTilemap = FindFirstObjectByType<Tilemap>();  // CS0618-safe

        if (floorTilemap)
            cellSize = floorTilemap.layoutGrid ? floorTilemap.layoutGrid.cellSize : floorTilemap.cellSize;
    }

    public IEnumerable<Health> Spawn()
    {
        if (!enemyPrefab) yield break;

        for (int i = 0; i < count; i++)
        {
            Vector3 pos = TrySampleValidPosition(out var p)
                ? (Vector3)p
                : transform.position; // fallback: guaranteed safe if you placed the marker well

            var h = Instantiate(enemyPrefab, pos, Quaternion.identity);
            yield return h;
        }
    }

    private bool TrySampleValidPosition(out Vector2 result)
    {
        // If radius is 0, just validate the marker position once.
        if (radius <= 0f)
        {
            result = transform.position;
            return IsOnFloor(result);
        }

        for (int attempt = 0; attempt < maxAttemptsPerEnemy; attempt++)
        {
            Vector2 candidate = (Vector2)transform.position + Random.insideUnitCircle * radius;
            if (IsOnFloor(candidate))
            {
                result = candidate;
                return true;
            }
        }

        // As a last resort, accept the marker itself if it’s on the floor.
        Vector2 fallback = transform.position;
        if (IsOnFloor(fallback))
        {
            result = fallback;
            return true;
        }

        // No valid spot found
        result = default;
        return false;
    }

    /// <summary>
    /// Returns true if the world position lands on a floor tile, and optionally
    /// if all neighbours within 'cellClearance' are also floor tiles.
    /// </summary>
    private bool IsOnFloor(Vector2 worldPos)
    {
        if (!floorTilemap) return true; // If no tilemap assigned, don’t block spawns.

        // Convert to cell space
        Vector3Int c = floorTilemap.WorldToCell(worldPos);

        // Quick reject: must be a tile here
        if (!floorTilemap.HasTile(c)) return false;

        if (cellClearance <= 0) return true;

        // Check a Chebyshev neighbourhood around the cell (a square region)
        for (int dx = -cellClearance; dx <= cellClearance; dx++)
        {
            for (int dy = -cellClearance; dy <= cellClearance; dy++)
            {
                var n = new Vector3Int(c.x + dx, c.y + dy, c.z);
                if (!floorTilemap.HasTile(n))
                    return false;
            }
        }

        return true;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.15f);
        Gizmos.DrawSphere(transform.position, Mathf.Max(0.01f, radius));

        // Marker
        Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, 0.075f);
    }
#endif
}