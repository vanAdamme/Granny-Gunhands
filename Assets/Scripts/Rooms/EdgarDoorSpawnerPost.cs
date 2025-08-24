using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Edgar.Unity;

/// Spawns 2×1 door prefabs at openings after generation (Grid2D).
/// Add this to LevelGenerator's "Custom post process tasks".
[AddComponentMenu("Granny/Edgar/Spawn 2x1 Doors (Post-Process)")]
public class EdgarDoorSpawnerPost : DungeonGeneratorPostProcessingComponentGrid2D
{
    [Header("Tilemap name needles (case-insensitive 'contains')")]
    [SerializeField] private string floorsNameContains = "floor";   // e.g., "ground"
    [SerializeField] private string wallsNameContains  = "walls";

    [Header("Door prefabs (2x1)")]
    [SerializeField] private Door verticalDoorPrefab;   // passage runs Up/Down (door spans Left–Right)
    [SerializeField] private Door horizontalDoorPrefab; // passage runs Left/Right (door spans Up–Down)
    [SerializeField] private Transform doorParent;      // optional container under generated Grid

    [Header("Behaviour")]
    [SerializeField] private bool detectExistingGaps = true; // true = do NOT edit tiles; just place doors
    [SerializeField, Min(1)] private int gapDepth = 1;       // if carving, thickness through walls
    [SerializeField] private TileBase floorDoorTile;         // optional: paint floor where we carve

    public override void Run(DungeonGeneratorLevelGrid2D level)
    {
        // 1) Find generated Grid + Floor/Walls tilemaps under the level root
        var root = level.RootGameObject != null ? level.RootGameObject.transform : transform;
        var grid = root.GetComponentInChildren<Grid>(true);
        if (grid == null)
        {
            Debug.LogWarning("[EdgarDoorSpawnerPost] No Grid under generated level root.");
            return;
        }

        Tilemap floors = null, walls = null;
        var maps = grid.GetComponentsInChildren<Tilemap>(true);
        string fNeedle = floorsNameContains.ToLowerInvariant();
        string wNeedle = wallsNameContains.ToLowerInvariant();

        foreach (var tm in maps)
        {
            var n = tm.name.ToLowerInvariant();
            if (floors == null && n.Contains(fNeedle)) floors = tm;
            if (walls  == null && n.Contains(wNeedle))  walls  = tm;
        }

        if (floors == null || walls == null)
        {
            Debug.LogWarning($"[EdgarDoorSpawnerPost] Could not find Floor/Walls tilemaps. Looked for '{floorsNameContains}', '{wallsNameContains}'.");
            return;
        }

        // 2) Detect two-wide 2x1 openings
        var openings = FindTwoWideOpenings(floors, walls);

        // 3) Optionally carve (if your tiles don’t already make gaps)
        if (!detectExistingGaps && openings.Count > 0)
        {
            CarveOpenings(openings, floors, walls);
            if (floorDoorTile != null)
            {
                foreach (var o in openings) { floors.SetTile(o.a, floorDoorTile); floors.SetTile(o.b, floorDoorTile); }
            }
            walls.CompressBounds();
            floors.CompressBounds();
        }

        // 4) Spawn doors (choose vertical/horizontal prefab)
        if (openings.Count > 0)
            SpawnDoors(openings, walls);

        Debug.Log($"[EdgarDoorSpawnerPost] {(detectExistingGaps ? "Detected" : "Carved")} {openings.Count} two‑wide openings and spawned doors.");
    }

    // ---------- detection ----------
    private enum Orientation { Vertical, Horizontal }
    private struct Opening { public Vector3Int a, b; public Orientation orientation; }

    private List<Opening> FindTwoWideOpenings(Tilemap floors, Tilemap walls)
    {
        var result = new List<Opening>(64);

        // Cache floor cells for O(1) checks
        var floorSet = new HashSet<Vector3Int>();
        foreach (var p in floors.cellBounds.allPositionsWithin)
            if (floors.HasTile(p)) floorSet.Add(p);

        var visited = new HashSet<Vector3Int>();
        Vector3Int L = new(-1, 0, 0), R = new(1, 0, 0), U = new(0, 1, 0), D = new(0, -1, 0);

        var wb = walls.cellBounds;
        foreach (var p in wb.allPositionsWithin)
        {
            if (visited.Contains(p)) continue;

            // Vertical passage (door spans Left<->Right): pair p & p+R are walls, with floor above both and below both.
            if (walls.HasTile(p) && walls.HasTile(p + R))
            {
                bool floorAbove = floorSet.Contains(p + U) && floorSet.Contains(p + R + U);
                bool floorBelow = floorSet.Contains(p + D) && floorSet.Contains(p + R + D);

                if (floorAbove && floorBelow)
                {
                    // canonical start: avoid duplicates if there’s also a valid pair anchored at p-Right
                    bool leftAlsoPair =
                        walls.HasTile(p + L) && walls.HasTile(p) &&
                        floorSet.Contains(p + L + U) && floorSet.Contains(p + U) &&
                        floorSet.Contains(p + L + D) && floorSet.Contains(p + D);

                    if (!leftAlsoPair)
                    {
                        result.Add(new Opening { a = p, b = p + R, orientation = Orientation.Vertical });
                        visited.Add(p); visited.Add(p + R);
                        continue;
                    }
                }
            }

            // Horizontal passage (door spans Up<->Down): pair p & p+U are walls, with floor left & right of both.
            if (walls.HasTile(p) && walls.HasTile(p + U))
            {
                bool floorLeft  = floorSet.Contains(p + L) && floorSet.Contains(p + U + L);
                bool floorRight = floorSet.Contains(p + R) && floorSet.Contains(p + U + R);

                if (floorLeft && floorRight)
                {
                    bool downAlsoPair =
                        walls.HasTile(p + D) && walls.HasTile(p) &&
                        floorSet.Contains(p + D + L) && floorSet.Contains(p + L) &&
                        floorSet.Contains(p + D + R) && floorSet.Contains(p + R);

                    if (!downAlsoPair)
                    {
                        result.Add(new Opening { a = p, b = p + U, orientation = Orientation.Horizontal });
                        visited.Add(p); visited.Add(p + U);
                        continue;
                    }
                }
            }
        }

        return result;
    }

    // ---------- carving (optional) ----------
    private void CarveOpenings(List<Opening> openings, Tilemap floors, Tilemap walls)
    {
        foreach (var o in openings)
        {
            // clear the two core wall tiles
            walls.SetTile(o.a, null);
            walls.SetTile(o.b, null);

            // add thickness perpendicular to the passage
            if (gapDepth > 1)
            {
                if (o.orientation == Orientation.Vertical)
                {
                    for (int d = 1; d < gapDepth; d++)
                    {
                        TryClear(walls, o.a + new Vector3Int(-d, 0, 0));
                        TryClear(walls, o.b + new Vector3Int(+d, 0, 0));
                    }
                }
                else
                {
                    for (int d = 1; d < gapDepth; d++)
                    {
                        TryClear(walls, o.a + new Vector3Int(0, -d, 0));
                        TryClear(walls, o.b + new Vector3Int(0, +d, 0));
                    }
                }
            }
        }

        static void TryClear(Tilemap walls, Vector3Int c)
        {
            if (walls.HasTile(c)) walls.SetTile(c, null);
        }
    }

    // ---------- spawning ----------
    private void SpawnDoors(List<Opening> openings, Tilemap walls)
    {
        var parent = doorParent ? doorParent : (walls ? walls.transform : transform);
        var cellSize = (walls && walls.layoutGrid) ? walls.layoutGrid.cellSize : Vector3.one;

        foreach (var o in openings)
        {
            var aCenter = walls.GetCellCenterWorld(o.a);
            var bCenter = walls.GetCellCenterWorld(o.b);
            var center  = (aCenter + bCenter) * 0.5f;

            var prefab = o.orientation == Orientation.Vertical ? verticalDoorPrefab : horizontalDoorPrefab;
            if (prefab == null) continue;

            var door = Instantiate(prefab, center, Quaternion.identity, parent);

            // ensure collider spans the 2×1 opening
            if (!door.TryGetComponent<BoxCollider2D>(out var col))
                col = door.gameObject.AddComponent<BoxCollider2D>();

            const float thickness = 0.9f; // thin across passage axis
            if (o.orientation == Orientation.Vertical)
                col.size = new Vector2(cellSize.x * 2f, cellSize.y * thickness); // spans left–right
            else
                col.size = new Vector2(cellSize.x * thickness, cellSize.y * 2f); // spans up–down
        }
    }
}