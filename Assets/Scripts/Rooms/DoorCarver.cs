using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[ExecuteAlways]
public class DoorCarver : MonoBehaviour
{
    [Header("Tilemaps (same Grid)")]
    public Tilemap floors;
    public Tilemap walls;
    [Tooltip("Optional: paint cleared doorway cells as floor")]
    public TileBase floorDoorTile;

    [Header("Door Settings")]
    [Min(1)] public int gapDepth = 1;      // how deep to punch through thick walls
    public bool widenToTwo = false;        // double-wide opening

    [Header("Door Prefab (optional)")]
    public bool spawnDoors = false;
    public Door doorPrefab;                // expects your Door script with BoxCollider2D
    public Transform doorParent;           // optional container; defaults to walls.transform

    [ContextMenu("Carve Doors")]
    public void Carve()
    {
        if (!floors || !walls) { Debug.LogError("[DoorCarver] Assign floors & walls."); return; }

        // Cache floor cells
        var floor = new HashSet<Vector3Int>();
        foreach (var p in floors.cellBounds.allPositionsWithin)
            if (floors.HasTile(p)) floor.Add(p);

        var toClear = new HashSet<Vector3Int>();
        var toFill  = new HashSet<Vector3Int>();

        // For spawning: keep unique “openings” with orientation and span
        var openings = new List<Opening>();

        Vector3Int LEFT  = new(-1, 0, 0);
        Vector3Int RIGHT = new( 1, 0, 0);
        Vector3Int UP    = new( 0, 1, 0);
        Vector3Int DOWN  = new( 0,-1, 0);

        var b = walls.cellBounds;
        var visited = new HashSet<Vector3Int>(); // avoid grouping the same opening twice

        foreach (var p in b.allPositionsWithin)
        {
            if (!walls.HasTile(p) || visited.Contains(p)) continue;

            bool horizDoor = floor.Contains(p + LEFT)  && floor.Contains(p + RIGHT);  // “door crosses horizontally” (vertical wall)
            bool vertDoor  = floor.Contains(p + UP)    && floor.Contains(p + DOWN);   // “door crosses vertically”   (horizontal wall)
            if (!horizDoor && !vertDoor) continue;

            // Core cell always cleared
            MarkClear(p);

            // Optional widening: add the neighbour cells perpendicular to passage axis
            if (widenToTwo)
            {
                if (horizDoor) { MarkClear(p + UP);   MarkClear(p + DOWN); }
                if (vertDoor)  { MarkClear(p + LEFT); MarkClear(p + RIGHT); }
            }

            // Punch through thick walls perpendicular to the passage
            if (vertDoor)
            {
                for (int d = 1; d < gapDepth; d++) { MarkClear(p + LEFT * d); MarkClear(p + RIGHT * d); }
            }
            else if (horizDoor)
            {
                for (int d = 1; d < gapDepth; d++) { MarkClear(p + UP * d); MarkClear(p + DOWN * d); }
            }

            // Record an opening once per doorway “center”
            var open = new Opening
            {
                cell = p,
                orientation = vertDoor ? Orientation.Vertical : Orientation.Horizontal,
                widthCells = widenToTwo ? 2 : 1
            };
            openings.Add(open);

            // Mark the cells that belong to this opening as visited (prevents duplicates)
            visited.Add(p);
            if (widenToTwo)
            {
                if (open.orientation == Orientation.Vertical) { visited.Add(p + LEFT);  visited.Add(p + RIGHT); }
                else { visited.Add(p + UP); visited.Add(p + DOWN); }
            }
        }

        // Apply tile edits
        foreach (var c in toClear) walls.SetTile(c, null);
        if (floorDoorTile) foreach (var f in toFill) floors.SetTile(f, floorDoorTile);
        walls.CompressBounds();
        floors.CompressBounds();
        Debug.Log($"[DoorCarver] Cleared {toClear.Count} wall tiles. Openings: {openings.Count}");

        // Optionally spawn Door prefabs that your RoomController can lock/unlock
        if (spawnDoors && doorPrefab)
            SpawnDoors(openings);

        // --- local helpers ---
        void MarkClear(Vector3Int pos)
        {
            if (!walls.HasTile(pos)) return;
            toClear.Add(pos);
            if (floorDoorTile) toFill.Add(pos);
        }
    }

    // ---------- Door spawning ----------
    enum Orientation { Horizontal, Vertical }
    struct Opening { public Vector3Int cell; public Orientation orientation; public int widthCells; }

    void SpawnDoors(List<Opening> openings)
    {
        if (openings == null || openings.Count == 0) return;
        var parent = doorParent ? doorParent : walls ? walls.transform : transform;
        var size = walls.layoutGrid.cellSize; // assumes square cells in top-down

        foreach (var o in openings)
        {
            // World-space center of the “core” door cell
            var center = walls.GetCellCenterWorld(o.cell);

            // Build a door
            var door = Instantiate(doorPrefab, center, Quaternion.identity, parent);

            // Ensure the collider exists (per your Door.cs)
            if (!door.TryGetComponent<BoxCollider2D>(out var col))
                col = door.gameObject.AddComponent<BoxCollider2D>();

            // Size the collider to cover the opening
            // We keep the blocker thin along the passage axis so it feels like a line across the gap.
            const float thickness = 0.9f; // ~one tile thick across the gap
            if (o.orientation == Orientation.Vertical)
            {
                // Passage runs up/down; door spans left-right
                col.size = new Vector2(o.widthCells * size.x, thickness * size.y);
                col.offset = Vector2.zero;
                door.transform.rotation = Quaternion.identity;
            }
            else // Horizontal
            {
                // Passage runs left/right; door spans up-down
                col.size = new Vector2(thickness * size.x, o.widthCells * size.y);
                col.offset = Vector2.zero;
                door.transform.rotation = Quaternion.identity;
            }

            // Optional: give them readable names in Hierarchy
            door.name = $"Door {o.orientation} {o.cell.x},{o.cell.y}";
        }
    }
}