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
    [SerializeField] private Transform doorParentOverride; // leave null to parent near the carved walls

    [Header("Behaviour")]
    [Tooltip("true = do NOT edit tiles; just place doors")]
    [SerializeField] private bool detectExistingGaps = true;
    [SerializeField, Min(1)] private int gapDepth = 1;
    [SerializeField] private TileBase floorDoorTile;

    private enum Orientation { Vertical, Horizontal }

    private struct Opening
    {
        public Vector3Int a, b;
        public Orientation orientation;
    }

    public override void Run(DungeonGeneratorLevelGrid2D level)
    {
        var root = level.RootGameObject ? level.RootGameObject.transform : null;
        if (!root) { Debug.LogWarning("[DoorPost] No generated level root."); return; }

        // Scan ALL tilemaps under the level root (there can be multiple Grids).
        var all = root.GetComponentsInChildren<Tilemap>(true);
        if (all == null || all.Length == 0) { Debug.LogWarning("[DoorPost] No Tilemaps found under generated level."); return; }

        string fNeedle = floorsNameContains.ToLowerInvariant();
        string wNeedle = wallsNameContains.ToLowerInvariant();

        // ---------- path / name helpers ----------
        string PathOf(Transform t)
        {
            var parts = new List<string>(8);
            for (var c = t; c != null; c = c.parent) parts.Add(c.name.ToLowerInvariant());
            parts.Reverse();
            return string.Join("/", parts);
        }
        bool NameOrParentContains(Tilemap tm, string needle)
        {
            var n = tm.name.ToLowerInvariant();
            var p = tm.transform.parent ? tm.transform.parent.name.ToLowerInvariant() : "";
            return n.Contains(needle) || p.Contains(needle);
        }
        bool IsRenderOrNonEmpty(Tilemap tm)
            => tm.name.ToLowerInvariant().Contains("rendertilemap") || CountTilesFast(tm) > 0;

        // ---------- classify floors by owner (rooms vs corridors) ----------
        bool IsRoomPath(string p)         => p.Contains("/rooms/");
        bool IsCorridorPathLoose(string p)=> p.Contains("corridor");
        bool IsGlobalShared(string p)     => p.Contains("/generated level/tilemaps/");

        var roomFloorMaps     = new List<Tilemap>();
        var corridorFloorMaps = new List<Tilemap>();
        var sampleRooms       = new List<string>(4);
        var sampleCorrs       = new List<string>(4);

        foreach (var tm in all)
        {
            if (!NameOrParentContains(tm, fNeedle)) continue;
            if (!IsRenderOrNonEmpty(tm)) continue;

            var path = PathOf(tm.transform);

            // Ignore the global merged floors for classification
            if (IsGlobalShared(path)) continue;

            if (IsCorridorPathLoose(path))
            {
                corridorFloorMaps.Add(tm);
                if (sampleCorrs.Count < 3) sampleCorrs.Add(path);
            }
            else // default to rooms
            {
                roomFloorMaps.Add(tm);
                if (sampleRooms.Count < 3) sampleRooms.Add(path);
            }
        }

        if (roomFloorMaps.Count == 0 && corridorFloorMaps.Count == 0)
        {
            Debug.LogWarning("[DoorPost] No per-room/corridor floor maps located.");
            return;
        }
        if (corridorFloorMaps.Count == 0)
        {
            Debug.LogWarning("[DoorPost] No corridor floor maps found (classifier looks for 'corridor' in hierarchy).\n" +
                             "Examples seen as rooms:\n  - " + string.Join("\n  - ", sampleRooms));
            return;
        }
        if (roomFloorMaps.Count == 0)
        {
            Debug.LogWarning("[DoorPost] No room floor maps found.");
            return;
        }

        Debug.Log($"[DoorPost] Room floors from {roomFloorMaps.Count} map(s). " +
                  (sampleRooms.Count > 0 ? $"e.g. '{sampleRooms[0]}'" : ""));
        Debug.Log($"[DoorPost] Corridor floors from {corridorFloorMaps.Count} map(s). " +
                  (sampleCorrs.Count > 0 ? $"e.g. '{sampleCorrs[0]}'" : ""));

        // Build union sets (rooms & corridors)
        var roomFloors     = BuildFloorSet(roomFloorMaps);
        var corridorFloors = BuildFloorSet(corridorFloorMaps);

        // ---------- collect ALL walls render maps; prefer the global, but process every one ----------
        var wallMaps = new List<Tilemap>();
        foreach (var tm in all)
        {
            if (!NameOrParentContains(tm, wNeedle)) continue;
            if (!IsRenderOrNonEmpty(tm)) continue;
            wallMaps.Add(tm);
        }
        if (wallMaps.Count == 0)
        {
            Debug.LogWarning($"[DoorPost] No walls maps found matching '{wallsNameContains}'.");
            return;
        }

        // Sort so the global consolidated walls map (if present) runs first
        wallMaps.Sort((a, b) =>
        {
            int Score(Tilemap tm)
            {
                var path = PathOf(tm.transform);
                int s = 0;
                if (path.Contains("/generated level/tilemaps/")) s += 10000;
                if (tm.name.ToLowerInvariant().Contains("rendertilemap")) s += 1000;
                s += CountTilesFast(tm);
                return -s; // lower is better for Sort()
            }
            return Score(a).CompareTo(Score(b));
        });

        int total = 0;
        var spawned = 0;

        // Process EACH walls map; carve only where room↔corridor meet
        foreach (var walls in wallMaps)
        {
            var path = PathOf(walls.transform);
            var openings = FindRoomCorridorOpenings(roomFloors, corridorFloors, walls);
            if (openings.Count == 0) { continue; }

            Debug.Log($"[DoorPost] {path} → {openings.Count} openings.");

            if (!detectExistingGaps)
            {
                CarveOpenings(openings, walls);

                if (floorDoorTile && roomFloorMaps.Count > 0)
                {
                    // paint a small threshold on the first room floor map
                    var paint = roomFloorMaps[0];
                    foreach (var o in openings) { paint.SetTile(o.a, floorDoorTile); paint.SetTile(o.b, floorDoorTile); }
                    paint.CompressBounds();
                }

                walls.CompressBounds();
            }

            // spawn doors using THIS walls map for positioning
            spawned += SpawnDoors(openings, walls, root);
            total   += openings.Count;
        }

        Debug.Log($"[DoorPost] {(detectExistingGaps ? "Detected" : "Carved")} {total} opening(s). Spawned {spawned} door(s). Done.");
    }

    // ---------- helpers ----------

    private static HashSet<Vector3Int> BuildFloorSet(List<Tilemap> maps)
    {
        var set = new HashSet<Vector3Int>();
        for (int i = 0; i < maps.Count; i++)
        {
            var tm = maps[i];
            foreach (var p in tm.cellBounds.allPositionsWithin)
                if (tm.HasTile(p)) set.Add(p);
        }
        return set;
    }

    private static int CountTilesFast(Tilemap tm)
    {
        var b = tm.cellBounds;
        foreach (var p in b.allPositionsWithin)
            if (tm.HasTile(p)) return 1;
        return 0;
    }

    // Detect 2×1 wall pairs where one side is ROOM floor and the other is CORRIDOR floor.
    private List<Opening> FindRoomCorridorOpenings(HashSet<Vector3Int> rooms, HashSet<Vector3Int> corridors, Tilemap walls)
    {
        var result  = new List<Opening>(64);
        var visited = new HashSet<Vector3Int>();

        Vector3Int L = new(-1, 0, 0), R = new(1, 0, 0), U = new(0, 1, 0), D = new(0, -1, 0);

        foreach (var p in walls.cellBounds.allPositionsWithin)
        {
            if (visited.Contains(p) || !walls.HasTile(p)) continue;

            // Vertical pair [p][p+R]
            if (walls.HasTile(p + R))
            {
                bool aboveRoom = rooms.Contains(p + U) && rooms.Contains(p + R + U);
                bool aboveCorr = corridors.Contains(p + U) && corridors.Contains(p + R + U);
                bool belowRoom = rooms.Contains(p + D) && rooms.Contains(p + R + D);
                bool belowCorr = corridors.Contains(p + D) && corridors.Contains(p + R + D);

                // Must be room on one side, corridor on the other
                bool valid = (aboveRoom && belowCorr) || (aboveCorr && belowRoom);

                // additionally, require that *outside* of the pair is not also floor of mixed ownership,
                // to avoid catching interior borders / corners
                bool notTriple = !walls.HasTile(p + L) && !walls.HasTile(p + R + R);

                if (valid && notTriple)
                {
                    result.Add(new Opening { a = p, b = p + R, orientation = Orientation.Vertical });
                    visited.Add(p); visited.Add(p + R);
                    continue;
                }
            }

            // Horizontal pair [p][p+U]
            if (walls.HasTile(p + U))
            {
                bool leftRoom  = rooms.Contains(p + L)     && rooms.Contains(p + U + L);
                bool leftCorr  = corridors.Contains(p + L) && corridors.Contains(p + U + L);
                bool rightRoom = rooms.Contains(p + R)     && rooms.Contains(p + U + R);
                bool rightCorr = corridors.Contains(p + R) && corridors.Contains(p + U + R);

                bool valid = (leftRoom && rightCorr) || (leftCorr && rightRoom);

                bool notTriple = !walls.HasTile(p + D) && !walls.HasTile(p + U + U);

                if (valid && notTriple)
                {
                    result.Add(new Opening { a = p, b = p + U, orientation = Orientation.Horizontal });
                    visited.Add(p); visited.Add(p + U);
                    continue;
                }
            }
        }

        return result;
    }

    private void CarveOpenings(List<Opening> openings, Tilemap walls)
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

        static void TryClear(Tilemap tm, Vector3Int c)
        {
            if (tm.HasTile(c)) tm.SetTile(c, null);
        }
    }

    private int SpawnDoors(List<Opening> openings, Tilemap ownerWalls, Transform levelRoot)
    {
        int spawned = 0;
        var parent   = doorParentOverride ? doorParentOverride : (ownerWalls ? ownerWalls.layoutGrid.transform : levelRoot);
        var cellSize = ownerWalls && ownerWalls.layoutGrid ? ownerWalls.layoutGrid.cellSize : Vector3.one;

        foreach (var o in openings)
        {
            var aCenter = ownerWalls.GetCellCenterWorld(o.a);
            var bCenter = ownerWalls.GetCellCenterWorld(o.b);
            var center  = (aCenter + bCenter) * 0.5f;

            var prefab = o.orientation == Orientation.Vertical ? verticalDoorPrefab : horizontalDoorPrefab;
            if (!prefab) continue;

            var door = Object.Instantiate(prefab, center, Quaternion.identity, parent);

            if (!door.TryGetComponent<BoxCollider2D>(out var col))
                col = door.gameObject.AddComponent<BoxCollider2D>();

            const float thickness = 0.9f;
            if (o.orientation == Orientation.Vertical)
                col.size = new Vector2(cellSize.x * 2f, cellSize.y * thickness);
            else
                col.size = new Vector2(cellSize.x * thickness, cellSize.y * 2f);

            spawned++;
        }

        return spawned;
    }
}

public static class TransformPathExt
{
    public static string GetHierarchyPath(this Transform t)
    {
        if (!t) return "<null>";
        var parts = new List<string>(8);
        for (var cur = t; cur != null; cur = cur.parent) parts.Add(cur.name);
        parts.Reverse();
        return string.Join("/", parts);
    }
}