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

    [Header("Doorway Settings")]
    [Min(1)] public int gapDepth = 1;      // how deep to punch through thick walls (perpendicular to passage)
    [Tooltip("If true, do not carve tiles; only detect existing 2x1 gaps and spawn doors.")]
    public bool detectExistingGaps = false;

    [Header("Spawn Door Prefab")]
    public bool spawnDoors = true;
    public Door doorPrefab;                // your Door.cs (enables/disables BoxCollider2D + layer) 
    public Transform doorParent;           // optional container; defaults to walls.transform

    enum Orientation { Vertical, Horizontal } // Vertical passage (up/down) or Horizontal passage (left/right)
    struct Opening
    {
        public Vector3Int a;               // first cell of the 2x1 pair (grid coords)
        public Vector3Int b;               // second cell of the 2x1 pair
        public Orientation orientation;    // passage orientation
    }

    [ContextMenu("Carve & Spawn Doors")]
    public void Carve()
    {
        if (!floors || !walls)
        {
            Debug.LogError("[DoorCarver] Assign floors & walls.");
            return;
        }

        // 1) Find all 2x1 doorway candidates
        var openings = FindTwoWideOpenings();

        // 2) Optionally clear the wall tiles to create/guarantee those openings
        if (!detectExistingGaps)
            CarveOpenings(openings);

        // 3) Optionally paint the floor tile in the cleared cells
        if (!detectExistingGaps && floorDoorTile)
        {
            foreach (var o in openings)
            {
                floors.SetTile(o.a, floorDoorTile);
                floors.SetTile(o.b, floorDoorTile);
            }
        }

        // Compress once after edits
        if (!detectExistingGaps)
        {
            walls.CompressBounds();
            floors.CompressBounds();
        }

        // 4) Spawn Door prefabs aligned to the 2x1 gap
        if (spawnDoors && doorPrefab != null && openings.Count > 0)
            SpawnDoors(openings);

        Debug.Log($"[DoorCarver] {(detectExistingGaps ? "Detected" : "Carved")} {openings.Count} two‑wide openings.");
    }

    // -------------------------------------------------------
    // 2×1 opening detection
    // -------------------------------------------------------
    List<Opening> FindTwoWideOpenings()
    {
        var openings = new List<Opening>(64);

        // For quick “is floor?” checks
        var floorSet = new HashSet<Vector3Int>();
        foreach (var p in floors.cellBounds.allPositionsWithin)
            if (floors.HasTile(p)) floorSet.Add(p);

        // Track visited to avoid double counting (we always start pairs at their "min" cell)
        var visited = new HashSet<Vector3Int>();

        Vector3Int L = new(-1, 0, 0), R = new(1, 0, 0), U = new(0, 1, 0), D = new(0, -1, 0);

        // Walk bounds where walls might exist (use walls bounds for speed)
        var wb = walls.cellBounds;
        foreach (var p in wb.allPositionsWithin)
        {
            if (visited.Contains(p)) continue;

            // --- Candidate A: VERTICAL PASSAGE (door spans left↔right) ---
            // Need a horizontal pair of wall cells: p and p+R are walls,
            // with FLOOR above both and FLOOR below both (so you pass through up/down).
            if (walls.HasTile(p) && walls.HasTile(p + R))
            {
                bool floorAbove = floorSet.Contains(p + U) && floorSet.Contains(p + R + U);
                bool floorBelow = floorSet.Contains(p + D) && floorSet.Contains(p + R + D);

                if (floorAbove && floorBelow)
                {
                    // ensure canonical start: only accept if p-Right is NOT a pair (avoids duplicates)
                    if (!(walls.HasTile(p + L) && walls.HasTile(p) &&
                          floorSet.Contains(p + L + U) && floorSet.Contains(p + U) &&
                          floorSet.Contains(p + L + D) && floorSet.Contains(p + D)))
                    {
                        openings.Add(new Opening { a = p, b = p + R, orientation = Orientation.Vertical });
                        visited.Add(p);
                        visited.Add(p + R);
                        continue;
                    }
                }
            }

            // --- Candidate B: HORIZONTAL PASSAGE (door spans up↕down) ---
            // Need a vertical pair: p and p+U are walls,
            // with FLOOR left & right of both (so you pass through left/right).
            if (walls.HasTile(p) && walls.HasTile(p + U))
            {
                bool floorLeft  = floorSet.Contains(p + L) && floorSet.Contains(p + U + L);
                bool floorRight = floorSet.Contains(p + R) && floorSet.Contains(p + U + R);

                if (floorLeft && floorRight)
                {
                    // canonical start: only accept if p-Up is NOT a pair
                    if (!(walls.HasTile(p + D) && walls.HasTile(p) &&
                          floorSet.Contains(p + D + L) && floorSet.Contains(p + L) &&
                          floorSet.Contains(p + D + R) && floorSet.Contains(p + R)))
                    {
                        openings.Add(new Opening { a = p, b = p + U, orientation = Orientation.Horizontal });
                        visited.Add(p);
                        visited.Add(p + U);
                        continue;
                    }
                }
            }
        }

        return openings;
    }

    // -------------------------------------------------------
    // Carve the 2x1 holes + thickness (gapDepth)
    // -------------------------------------------------------
    void CarveOpenings(List<Opening> openings)
    {
        if (openings == null || openings.Count == 0) return;

        foreach (var o in openings)
        {
            // Clear the two core wall tiles
            walls.SetTile(o.a, null);
            walls.SetTile(o.b, null);

            // Add thickness perpendicular to the passage direction
            // (i.e., punch deeper into a thick wall)
            if (gapDepth > 1)
            {
                if (o.orientation == Orientation.Vertical)
                {
                    // Punch LEFT and RIGHT from both cells
                    for (int d = 1; d < gapDepth; d++)
                    {
                        TryClear(o.a + new Vector3Int(-d, 0, 0));
                        TryClear(o.b + new Vector3Int(+d, 0, 0));
                    }
                }
                else // Horizontal passage
                {
                    // Punch UP and DOWN from both cells
                    for (int d = 1; d < gapDepth; d++)
                    {
                        TryClear(o.a + new Vector3Int(0, -d, 0));
                        TryClear(o.b + new Vector3Int(0, +d, 0));
                    }
                }
            }
        }

        void TryClear(Vector3Int c)
        {
            if (walls.HasTile(c)) walls.SetTile(c, null);
        }
    }

    // -------------------------------------------------------
    // Spawn 1 Door prefab centered across each 2x1 opening
    // -------------------------------------------------------
    void SpawnDoors(List<Opening> openings)
    {
        var parent = doorParent ? doorParent : (walls ? walls.transform : transform);
        var cellSize = (walls && walls.layoutGrid) ? walls.layoutGrid.cellSize : Vector3.one;

        foreach (var o in openings)
        {
            // World center between the two cells
            var aCenter = walls.GetCellCenterWorld(o.a);
            var bCenter = walls.GetCellCenterWorld(o.b);
            var center = (aCenter + bCenter) * 0.5f;

            var door = Instantiate(doorPrefab, center, Quaternion.identity, parent);

            // Ensure there is a BoxCollider2D and size it to span the opening
            if (!door.TryGetComponent<BoxCollider2D>(out var col))
                col = door.gameObject.AddComponent<BoxCollider2D>();

            const float thickness = 0.9f; // thin across passage axis, nearly full along span
            if (o.orientation == Orientation.Vertical)
            {
                // Door spans left↔right (2 tiles wide), thin in Y
                col.size = new Vector2(cellSize.x * 2f, cellSize.y * thickness);
            }
            else // Horizontal
            {
                // Door spans up↕down (2 tiles tall), thin in X
                col.size = new Vector2(cellSize.x * thickness, cellSize.y * 2f);
            }

            door.name = $"Door {o.orientation} ({o.a.x},{o.a.y})-({o.b.x},{o.b.y})";
        }
    }
}
