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
    [Min(1)] public int gapDepth = 1; // punch-through thickness
    public bool widenToTwo = false;

    static readonly Vector3Int[] D4 = {
        new Vector3Int( 1, 0, 0), new Vector3Int(-1, 0, 0),
        new Vector3Int( 0, 1, 0), new Vector3Int( 0,-1, 0),
    };

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

        // Direction constants
        Vector3Int LEFT  = new Vector3Int(-1, 0, 0);
        Vector3Int RIGHT = new Vector3Int( 1, 0, 0);
        Vector3Int UP    = new Vector3Int( 0, 1, 0);
        Vector3Int DOWN  = new Vector3Int( 0,-1, 0);

        // Walk every wall cell and see if it separates floor on opposite sides
        var b = walls.cellBounds;
        foreach (var p in b.allPositionsWithin)
        {
            if (!walls.HasTile(p)) continue;

            bool horizDoor = floor.Contains(p + LEFT)  && floor.Contains(p + RIGHT);
            bool vertDoor  = floor.Contains(p + UP)    && floor.Contains(p + DOWN);

            if (!horizDoor && !vertDoor) continue;

            // Clear the core tile
            toClear.Add(p);
            if (floorDoorTile) toFill.Add(p);

            // Optional widening (one tile) around the core
            if (widenToTwo)
            {
                if (horizDoor) { Try(p + UP);   Try(p + DOWN); }
                if (vertDoor)  { Try(p + LEFT); Try(p + RIGHT); }
            }

            // Punch through thicker walls along the axis perpendicular to the door
            if (vertDoor)
            {
                for (int d = 1; d < gapDepth; d++)
                {
                    Try(p + LEFT  * d);
                    Try(p + RIGHT * d);
                }
            }
            else if (horizDoor)
            {
                for (int d = 1; d < gapDepth; d++)
                {
                    Try(p + UP   * d);
                    Try(p + DOWN * d);
                }
            }
        }

        foreach (var c in toClear) walls.SetTile(c, null);
        if (floorDoorTile) foreach (var f in toFill) floors.SetTile(f, floorDoorTile);

        walls.CompressBounds();
        floors.CompressBounds();
        Debug.Log($"[DoorCarver] Cleared {toClear.Count} wall tiles.");

        void Try(Vector3Int pos)
        {
            if (!walls.HasTile(pos)) return;
            toClear.Add(pos);
            if (floorDoorTile) toFill.Add(pos);
        }
    }
}