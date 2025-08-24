using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Edgar.Unity;

[CreateAssetMenu(menuName = "Edgar/Post-processing/Carve doors from Edgar door positions (Unified Grid)", fileName = "Spawn2x1DoorsPostProcessing")]
public class EdgarDoorSpawnerPostProcessing : DungeonGeneratorPostProcessingGrid2D
{
    [Header("Tilemap name needles (case-insensitive 'contains')")]
    [SerializeField] private string floorsNameContains = "floor";
    [SerializeField] private string wallsNameContains  = "walls";

    [Header("Behaviour")]
    [Tooltip("Detect only = do not modify tiles (just logs counts).")]
    [SerializeField] private bool detectOnly = false;

    [Tooltip("Also clear the second wall tile if present (typical 2-thick borders).")]
    [SerializeField] private bool clearSecondWallIfPresent = true;

    [Tooltip("Optional: paint a tile on the doorway floor (first room floor map).")]
    [SerializeField] private TileBase floorDoorTile;

    [Header("Optional door prefabs (OFF while validating carving)")]
    [SerializeField] private bool spawnDoorPrefabs = false;
    [SerializeField] private Door verticalDoorPrefab;
    [SerializeField] private Door horizontalDoorPrefab;
    [SerializeField] private Transform doorParentOverride;

    private enum Orientation { Vertical, Horizontal }

    private struct Opening
    {
        public Vector3Int a, b;            // wall cells to clear in the REFERENCE grid
        public Orientation orientation;
    }

    public override void Run(DungeonGeneratorLevelGrid2D level)
    {
        var root = level.RootGameObject ? level.RootGameObject.transform : null;
        if (!root) { Debug.LogWarning("[DoorPost] No generated level root."); return; }

        // --- collect all Tilemaps under the level (multiple Grids exist) ---
        var all = root.GetComponentsInChildren<Tilemap>(true);
        if (all.Length == 0) { Debug.LogWarning("[DoorPost] No Tilemaps found."); return; }

        string fNeedle = floorsNameContains.ToLowerInvariant();
        string wNeedle = wallsNameContains.ToLowerInvariant();

        // Helpers
        static string PathOf(Transform t)
        {
            var parts = new List<string>(8);
            for (var c = t; c != null; c = c.parent) parts.Add(c.name.ToLowerInvariant());
            parts.Reverse();
            return string.Join("/", parts);
        }
        static bool NameOrParentContains(Tilemap tm, string needle)
        {
            var n = tm.name.ToLowerInvariant();
            var p = tm.transform.parent ? tm.transform.parent.name.ToLowerInvariant() : "";
            return n.Contains(needle) || p.Contains(needle);
        }
        static int CountTilesFast(Tilemap tm)
        {
            var b = tm.cellBounds;
            foreach (var p in b.allPositionsWithin)
                if (tm.HasTile(p)) return 1;
            return 0;
        }
        static bool IsRenderOrNonEmpty(Tilemap tm) =>
            tm.name.ToLowerInvariant().Contains("rendertilemap") || CountTilesFast(tm) > 0;

        // --- choose a REFERENCE GridLayout (global '/Generated Level/Tilemaps' preferred) ---
        GridLayout referenceGrid = null;
        for (int i = 0; i < all.Length; i++)
        {
            var tm = all[i];
            var path = PathOf(tm.transform);
            if (path.Contains("/generated level/tilemaps/")) { referenceGrid = tm.layoutGrid; break; }
        }
        if (!referenceGrid) referenceGrid = all[0].layoutGrid; // fallback to anything
        if (!referenceGrid) { Debug.LogWarning("[DoorPost] No reference grid found."); return; }

        Vector3Int ToRefCellTM(Tilemap tm, Vector3Int localCell)
            => referenceGrid.WorldToCell(tm.GetCellCenterWorld(localCell));

        Vector3Int ToRefCellGrid(GridLayout grid, Vector3Int localCell)
        {
            // Get world center for the cell on that grid, then convert to the reference grid
            Vector3 world = grid.CellToWorld(localCell);
            if (grid is Grid g) world += (Vector3)(g.cellSize * 0.5f);
            return referenceGrid.WorldToCell(world);
        }

        Vector3Int RefToLocal(Tilemap tm, Vector3Int refCell)
            => tm.layoutGrid.WorldToCell(referenceGrid.CellToWorld(refCell));

        // --- build floor sets (room vs corridor) in REFERENCE grid space ---
        var roomFloorMaps     = new List<Tilemap>();
        var corridorFloorMaps = new List<Tilemap>();
        foreach (var tm in all)
        {
            if (!NameOrParentContains(tm, fNeedle)) continue;
            if (!IsRenderOrNonEmpty(tm)) continue;
            var path = PathOf(tm.transform);
            if (path.Contains("/generated level/tilemaps/")) continue; // skip merged floors for classification
            if (path.Contains("corridor")) corridorFloorMaps.Add(tm);
            else                           roomFloorMaps.Add(tm);
        }
        if (roomFloorMaps.Count == 0 || corridorFloorMaps.Count == 0)
        {
            Debug.LogWarning($"[DoorPost] Missing floor maps. rooms={roomFloorMaps.Count}, corridors={corridorFloorMaps.Count}");
            return;
        }

        var roomFloorsRef     = BuildRefSet(roomFloorMaps, ToRefCellTM);
        var corridorFloorsRef = BuildRefSet(corridorFloorMaps, ToRefCellTM);
        Debug.Log($"[DoorPost] Room floors from {roomFloorMaps.Count} map(s). Corridor floors from {corridorFloorMaps.Count} map(s).");

        // --- collect walls + union set in REFERENCE grid space ---
        var wallMaps = new List<Tilemap>();
        foreach (var tm in all)
            if (NameOrParentContains(tm, wNeedle) && IsRenderOrNonEmpty(tm))
                wallMaps.Add(tm);
        if (wallMaps.Count == 0) { Debug.LogWarning($"[DoorPost] No walls maps found by '{wallsNameContains}'."); return; }

        var wallsRef = new HashSet<Vector3Int>();
        foreach (var w in wallMaps)
            foreach (var p in w.cellBounds.allPositionsWithin)
                if (w.HasTile(p)) wallsRef.Add(ToRefCellTM(w, p));

        // --- get corridor Doors components and convert their cells to REFERENCE grid space ---
        var doorComps = root.GetComponentsInChildren<DoorsGrid2D>(true);
        var doorCellsRef = new HashSet<Vector3Int>();
        foreach (var d in doorComps)
        {
            var doorPath = PathOf(d.transform);
            if (!(doorPath.Contains("/rooms/") && doorPath.Contains("corridor"))) continue; // corridor only

            var doorGrid = d.GetComponentInParent<GridLayout>();
            if (!doorGrid) continue;

            foreach (var seg in EnumerateDoorSegments(d))
                foreach (var c in EnumerateCells(seg.from, seg.to))
                    doorCellsRef.Add(ToRefCellGrid(doorGrid, c));
        }

        if (doorCellsRef.Count == 0)
        {
            Debug.LogWarning("[DoorPost] 0 door cells collected from corridor Doors components.");
            return;
        }

        // --- for each door-edge ref cell, clear adjacent room-side walls (in REF space) ---
        var openings = new List<Opening>(64);
        Vector3Int[] dirs = { Vector3Int.left, Vector3Int.right, Vector3Int.up, Vector3Int.down };

        foreach (var p in doorCellsRef)
        {
            foreach (var n in dirs)
            {
                // corridor at p-n, wall at p+n, room at p+2n (typical 2-thick), or at least room at p+n
                if (corridorFloorsRef.Contains(p - n) && wallsRef.Contains(p + n) &&
                    (roomFloorsRef.Contains(p + n + n) || roomFloorsRef.Contains(p + n)))
                {
                    var a = p + n;
                    var b = (clearSecondWallIfPresent && wallsRef.Contains(p + n + n)) ? (p + n + n) : a;
                    openings.Add(new Opening { a = a, b = b, orientation = (n.x != 0) ? Orientation.Vertical : Orientation.Horizontal });
                    break;
                }
                // mirrored
                if (corridorFloorsRef.Contains(p + n) && wallsRef.Contains(p - n) &&
                    (roomFloorsRef.Contains(p - n - n) || roomFloorsRef.Contains(p - n)))
                {
                    var a = p - n;
                    var b = (clearSecondWallIfPresent && wallsRef.Contains(p - n - n)) ? (p - n - n) : a;
                    openings.Add(new Opening { a = a, b = b, orientation = (n.x != 0) ? Orientation.Vertical : Orientation.Horizontal });
                    break;
                }
            }
        }

        // --- mutate tilemaps: convert REF cells back to each map’s local cells and clear ---
        int carved = 0;
        if (!detectOnly && openings.Count > 0)
        {
            foreach (var walls in wallMaps)
            {
                foreach (var o in openings)
                {
                    var aLocal = RefToLocal(walls, o.a);
                    var bLocal = RefToLocal(walls, o.b);

                    if (walls.HasTile(aLocal)) { walls.SetTile(aLocal, null); carved++; }
                    if (walls.HasTile(bLocal)) { walls.SetTile(bLocal, null); carved++; }
                }
                walls.CompressBounds();
            }

            if (floorDoorTile && roomFloorMaps.Count > 0)
            {
                var paint = roomFloorMaps[0];
                foreach (var o in openings)
                {
                    var aLocal = RefToLocal(paint, o.a);
                    var bLocal = RefToLocal(paint, o.b);
                    paint.SetTile(aLocal, floorDoorTile);
                    paint.SetTile(bLocal, floorDoorTile);
                }
                paint.CompressBounds();
            }
        }

        // --- optional prefabs (leave OFF until carving is verified) ---
        if (spawnDoorPrefabs && openings.Count > 0)
        {
            // position using any walls map’s grid; use the first global one if present
            Tilemap owner = null;
            foreach (var w in wallMaps)
                if (PathOf(w.transform).Contains("/generated level/tilemaps/")) { owner = w; break; }
            owner ??= wallMaps[0];

            var parent   = doorParentOverride ? doorParentOverride : (owner.layoutGrid ? owner.layoutGrid.transform : root);
            var cellSize = owner.layoutGrid ? owner.layoutGrid.cellSize : Vector3.one;

            foreach (var o in openings)
            {
                var aW = owner.layoutGrid.CellToWorld(RefToLocal(owner, o.a));
                var bW = owner.layoutGrid.CellToWorld(RefToLocal(owner, o.b));
                var center = (aW + bW) * 0.5f;

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

        Debug.Log($"[DoorPost] {(detectOnly ? "Detected" : "Carved")} {openings.Count} doorway(s); tiles cleared ~{carved}. " +
                  $"RefGrid='{referenceGrid.transform.GetHierarchyPath()}'.");
    }

    // ----- helpers -----

    private static HashSet<Vector3Int> BuildRefSet(List<Tilemap> maps, System.Func<Tilemap, Vector3Int, Vector3Int> toRef)
    {
        var set = new HashSet<Vector3Int>();
        foreach (var tm in maps)
            foreach (var p in tm.cellBounds.allPositionsWithin)
                if (tm.HasTile(p)) set.Add(toRef(tm, p));
        return set;
    }

    private static IEnumerable<(Vector3Int from, Vector3Int to)> EnumerateDoorSegments(DoorsGrid2D doors)
    {
        // Manual
        if (doors.SelectedMode == DoorsGrid2D.DoorMode.Manual && doors.ManualDoorModeData?.DoorsList != null)
        {
            foreach (var d in doors.ManualDoorModeData.DoorsList)
            {
                var a = new Vector3Int(Mathf.RoundToInt(d.From.x), Mathf.RoundToInt(d.From.y), 0);
                var b = new Vector3Int(Mathf.RoundToInt(d.To.x),   Mathf.RoundToInt(d.To.y),   0);
                yield return (a, b);
            }
            yield break;
        }

        // Hybrid
        if (doors.SelectedMode == DoorsGrid2D.DoorMode.Hybrid && doors.HybridDoorModeData?.DoorLines != null)
        {
            foreach (var l in doors.HybridDoorModeData.DoorLines)
                yield return (l.From, l.To);
            yield break;
        }

        // Simple
        if (doors.SelectedMode == DoorsGrid2D.DoorMode.Simple && doors.SimpleDoorModeData != null)
        {
            var lines = doors.SimpleDoorModeData.GetDoorLines(doors);
            if (lines != null)
                foreach (var l in lines)
                    yield return (l.From, l.To);
        }
    }

    private static IEnumerable<Vector3Int> EnumerateCells(Vector3Int a, Vector3Int b)
    {
        var dir = new Vector3Int(Mathf.Clamp(b.x - a.x, -1, 1), Mathf.Clamp(b.y - a.y, -1, 1), 0);
        if (dir == Vector3Int.zero) { yield return a; yield break; }

        var cur = a;
        yield return cur;
        while (cur != b)
        {
            cur += dir;
            yield return cur;
        }
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