using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Edgar.Unity;

[CreateAssetMenu(menuName = "Edgar/Post-processing/Spawn 2x1 Doors", fileName = "Spawn2x1DoorsPostProcessing")]
public class EdgarDoorSpawnerPostProcessing : DungeonGeneratorPostProcessingGrid2D
{
    [Header("Tilemap name needles (case-insensitive 'contains')")]
    [SerializeField] private string floorsNameContains = "floor";
    [SerializeField] private string wallsNameContains  = "walls";

    [Header("Door prefabs (2x1)")]
    [SerializeField] private Door verticalDoorPrefab;
    [SerializeField] private Door horizontalDoorPrefab;
    [SerializeField] private Transform doorParentOverride; // leave null to parent under generated Grid

    [Header("Behaviour")]
    [Tooltip("true = do NOT edit tiles; just place doors")]
    [SerializeField] private bool detectExistingGaps = true;
    [SerializeField, Min(1)] private int gapDepth = 1;
    [SerializeField] private TileBase floorDoorTile;

    private enum Orientation { Vertical, Horizontal }
    private struct Opening { public Vector3Int a, b; public Orientation orientation; }

    public override void Run(DungeonGeneratorLevelGrid2D level)
    {
        var root = level.RootGameObject != null ? level.RootGameObject.transform : null;
        var grid = root ? root.GetComponentInChildren<Grid>(true) : null;
        if (!grid) { Debug.LogWarning("[DoorPost] No Grid under generated level root."); return; }

        // Locate Floor/Walls tilemaps *on this grid*
        Tilemap floors = null, walls = null;
        string fNeedle = floorsNameContains.ToLowerInvariant();
        string wNeedle = wallsNameContains.ToLowerInvariant();

        foreach (var tm in grid.GetComponentsInChildren<Tilemap>(true))
        {
            var n = tm.name.ToLowerInvariant();
            if (!floors && n.Contains(fNeedle)) floors = tm;
            if (!walls  && n.Contains(wNeedle)) walls  = tm;
        }

        if (!floors || !walls)
        {
            Debug.LogWarning($"[DoorPost] Could not find Floor/Walls tilemaps. Needles '{floorsNameContains}' / '{wallsNameContains}'.");
            return;
        }

        var openings = FindTwoWideOpenings(floors, walls);
        Debug.Log($"[DoorPost] Detected {openings.Count} two‑wide openings.");

        if (openings.Count > 0 && !detectExistingGaps)
        {
            CarveOpenings(openings, floors, walls);

            if (floorDoorTile)
            {
                foreach (var o in openings)
                { floors.SetTile(o.a, floorDoorTile); floors.SetTile(o.b, floorDoorTile); }
            }

            walls.CompressBounds();
            floors.CompressBounds();
        }

        if (openings.Count > 0)
            SpawnDoors(openings, walls, root);

        Debug.Log($"[DoorPost] {(detectExistingGaps ? "Detected" : "Carved")} {openings.Count} openings. Done.");
    }

    private List<Opening> FindTwoWideOpenings(Tilemap floors, Tilemap walls)
    {
        var result  = new List<Opening>(64);
        var visited = new HashSet<Vector3Int>();
        var floorSet = new HashSet<Vector3Int>();

        foreach (var p in floors.cellBounds.allPositionsWithin)
            if (floors.HasTile(p)) floorSet.Add(p);

        Vector3Int L = new(-1,0,0), R = new(1,0,0), U = new(0,1,0), D = new(0,-1,0);

        foreach (var p in walls.cellBounds.allPositionsWithin)
        {
            if (visited.Contains(p) || !walls.HasTile(p)) continue;

            if (walls.HasTile(p + R))
            {
                bool above = floorSet.Contains(p + U) && floorSet.Contains(p + R + U);
                bool below = floorSet.Contains(p + D) && floorSet.Contains(p + R + D);
                if (above && below)
                {
                    bool leftAlsoPair = walls.HasTile(p + L) &&
                                        floorSet.Contains(p + L + U) && floorSet.Contains(p + L + D);
                    if (!leftAlsoPair)
                    {
                        result.Add(new Opening { a = p, b = p + R, orientation = Orientation.Vertical });
                        visited.Add(p); visited.Add(p + R);
                        continue;
                    }
                }
            }

            if (walls.HasTile(p + U))
            {
                bool left  = floorSet.Contains(p + L) && floorSet.Contains(p + U + L);
                bool right = floorSet.Contains(p + R) && floorSet.Contains(p + U + R);
                if (left && right)
                {
                    bool downAlsoPair = walls.HasTile(p + D) &&
                                        floorSet.Contains(p + D + L) && floorSet.Contains(p + D + R);
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

    private void CarveOpenings(List<Opening> openings, Tilemap floors, Tilemap walls)
    {
        foreach (var o in openings)
        {
            walls.SetTile(o.a, null);
            walls.SetTile(o.b, null);

            if (gapDepth > 1)
            {
                if (o.orientation == Orientation.Vertical)
                {
                    for (int d = 1; d < gapDepth; d++)
                    { TryClear(walls, o.a + new Vector3Int(-d,0,0)); TryClear(walls, o.b + new Vector3Int(+d,0,0)); }
                }
                else
                {
                    for (int d = 1; d < gapDepth; d++)
                    { TryClear(walls, o.a + new Vector3Int(0,-d,0)); TryClear(walls, o.b + new Vector3Int(0,+d,0)); }
                }
            }
        }

        static void TryClear(Tilemap tm, Vector3Int c) { if (tm.HasTile(c)) tm.SetTile(c, null); }
    }

    private void SpawnDoors(List<Opening> openings, Tilemap walls, Transform levelRoot)
    {
        var parent = doorParentOverride ? doorParentOverride : (walls ? walls.layoutGrid.transform : levelRoot);
        var cellSize = walls && walls.layoutGrid ? walls.layoutGrid.cellSize : Vector3.one;

        foreach (var o in openings)
        {
            var aCenter = walls.GetCellCenterWorld(o.a);
            var bCenter = walls.GetCellCenterWorld(o.b);
            var center  = (aCenter + bCenter) * 0.5f;

            var prefab = o.orientation == Orientation.Vertical ? verticalDoorPrefab : horizontalDoorPrefab;
            if (!prefab) continue;

            var door = Object.Instantiate(prefab, center, Quaternion.identity, parent);

            if (!door.TryGetComponent<BoxCollider2D>(out var col))
                col = door.gameObject.AddComponent<BoxCollider2D>();

            const float t = 0.9f;
            if (o.orientation == Orientation.Vertical)
                col.size = new Vector2(cellSize.x * 2f, cellSize.y * t);
            else
                col.size = new Vector2(cellSize.x * t, cellSize.y * 2f);
        }
    }
}