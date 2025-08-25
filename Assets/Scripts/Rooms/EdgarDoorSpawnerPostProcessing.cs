using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using Edgar.Unity;

[CreateAssetMenu(menuName = "Edgar/Post-processing/Carve 2x1 Doors (regular tilemaps)", fileName = "Spawn2x1DoorsPostProcessing")]
public class EdgarDoorSpawnerPostProcessing : DungeonGeneratorPostProcessingGrid2D
{
    [Header("Tilemap name needles (case-insensitive 'contains')")]
    [SerializeField] private string floorsNameContains  = "floor";
    [SerializeField] private string wallsNameContains   = "walls";
    [SerializeField] private string outlineNameContains = "outline";   // optional, cleared if present

    [Header("Behaviour")]
    [Tooltip("If ON, only logs; does not modify tiles.")]
    [SerializeField] private bool detectOnly = false;

    [Tooltip("Max rows of wall between corridor and room to clear (roomwards). 1 for thin walls.")]
    [SerializeField, Min(1)] private int maxWallThickness = 1;

    [Tooltip("Optional: paint a tile on the doorway floor (first room floor map).")]
    [SerializeField] private TileBase floorDoorTile;

    private enum Orientation { Vertical, Horizontal }

    private struct Opening
    {
        public Vector3Int a, b;     // first wall-row cells (reference grid)
        public Vector3Int n;        // normal pointing from corridor -> room
        public int depth;           // how many wall rows to clear inwards (1..maxWallThickness)
        public Orientation o;
    }

    public override void Run(DungeonGeneratorLevelGrid2D level)
    {
        var root = level.RootGameObject ? level.RootGameObject.transform : null;
        if (!root) { Debug.LogWarning("[DoorPost] No generated level root."); return; }

        // Collect all tilemaps once
        var all = root.GetComponentsInChildren<Tilemap>(true);
        if (all.Length == 0) { Debug.LogWarning("[DoorPost] No Tilemaps found."); return; }

        string fNeedle = floorsNameContains.ToLowerInvariant();
        string wNeedle = wallsNameContains.ToLowerInvariant();
        string oNeedle = outlineNameContains.ToLowerInvariant();

        // helpers
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
            CountTilesFast(tm) > 0 || tm.name.ToLowerInvariant().Contains("rendertilemap");

        // Choose a reference grid (prefer the global "/Generated Level/Tilemaps")
        GridLayout referenceGrid = null;
        foreach (var tm in all)
            if (PathOf(tm.transform).Contains("/generated level/tilemaps/")) { referenceGrid = tm.layoutGrid; break; }
        referenceGrid ??= all[0].layoutGrid;
        if (!referenceGrid) { Debug.LogWarning("[DoorPost] No GridLayout found."); return; }

        Vector3Int ToRefCellTM(Tilemap tm, Vector3Int localCell)
            => referenceGrid.WorldToCell(tm.GetCellCenterWorld(localCell));
        Vector3Int RefToLocal(Tilemap tm, Vector3Int refCell)
            => tm.layoutGrid.WorldToCell(referenceGrid.CellToWorld(refCell));

        // Classify room vs corridor floors (by hierarchy path)
        var roomFloorMaps     = new List<Tilemap>();
        var corridorFloorMaps = new List<Tilemap>();
        foreach (var tm in all)
        {
            if (!NameOrParentContains(tm, fNeedle)) continue;
            if (!IsRenderOrNonEmpty(tm)) continue;

            var path = PathOf(tm.transform);
            if (path.Contains("/generated level/tilemaps/")) continue; // skip merged/global, we want per-room/corridor

            if (path.Contains("corridor")) corridorFloorMaps.Add(tm);
            else                           roomFloorMaps.Add(tm);
        }
        if (roomFloorMaps.Count == 0 || corridorFloorMaps.Count == 0)
        {
            Debug.LogWarning($"[DoorPost] Missing floors. rooms={roomFloorMaps.Count}, corridors={corridorFloorMaps.Count}");
            return;
        }

        // Room/corridor floors as reference-cell sets
        var roomFloorsRef     = BuildRefSet(roomFloorMaps, ToRefCellTM);
        var corridorFloorsRef = BuildRefSet(corridorFloorMaps, ToRefCellTM);
        Debug.Log($"[DoorPost] Room floors from {roomFloorMaps.Count} map(s). Corridor floors from {corridorFloorMaps.Count} map(s).");

        // Gather all walls/outline maps (we will mutate these)
        var wallMaps = new List<Tilemap>();
        foreach (var tm in all)
        {
            if (!IsRenderOrNonEmpty(tm)) continue;

            var name = tm.name.ToLowerInvariant();
            var parent = tm.transform.parent ? tm.transform.parent.name.ToLowerInvariant() : "";
            var path = PathOf(tm.transform);

            bool looksLikeWalls   = name.Contains(wNeedle) || parent.Contains(wNeedle);
            bool looksLikeOutline = name.Contains(oNeedle) || parent.Contains(oNeedle) || path.Contains("/outline");

            if (looksLikeWalls || looksLikeOutline) wallMaps.Add(tm);
        }
        if (wallMaps.Count == 0)
        {
            Debug.LogWarning($"[DoorPost] No Walls/Outline maps found by '{wallsNameContains}'/'{outlineNameContains}'.");
            return;
        }

        // Union of all wall cells in reference space (for fast lookups)
        var wallsRef = new HashSet<Vector3Int>();
        foreach (var w in wallMaps)
            foreach (var p in w.cellBounds.allPositionsWithin)
                if (w.HasTile(p)) wallsRef.Add(ToRefCellTM(w, p));

        // --- Detect 2-wide door mouths strictly where (corridor ⟂ wall ⟂ room) ---
        // Mark candidate wall cells with their corridor->room normal
        var candNormal = new Dictionary<Vector3Int, Vector3Int>(); // ref cell -> normal
        Vector3Int[] N = { Vector3Int.left, Vector3Int.right, Vector3Int.up, Vector3Int.down };

        foreach (var w in wallsRef)
        {
            foreach (var n in N)
            {
                var plus  = w + n;
                var minus = w - n;

                bool corridorOppRoom =
                    (corridorFloorsRef.Contains(minus) && roomFloorsRef.Contains(plus)) ||
                    (corridorFloorsRef.Contains(plus)  && roomFloorsRef.Contains(minus));

                if (!corridorOppRoom) continue;

                // store normal pointing corridor -> room
                Vector3Int corrToRoom = corridorFloorsRef.Contains(minus) ? n : -n;

                // If multiple normals ever match (rare corner), prefer the first – pairs check will filter it out.
                if (!candNormal.ContainsKey(w)) candNormal[w] = corrToRoom;
                break;
            }
        }

        // Group into strict 2-wide pairs along tangent (no triples)
        var openings = new List<Opening>(64);
        var seen = new HashSet<Vector3Int>();

        foreach (var cell in candNormal.Keys)
        {
            if (seen.Contains(cell)) continue;

            var n = candNormal[cell];
            var t = (n.x != 0) ? Vector3Int.up : Vector3Int.right;

            // must have same-normal neighbor on +t
            if (!candNormal.TryGetValue(cell + t, out var n2) || n2 != n)
                continue;

            // start-of-run filter: previous on -t must not exist (same normal)
            if (candNormal.TryGetValue(cell - t, out var nPrev) && nPrev == n)
                continue;

            // block triples: a third on +t+t with same normal -> skip
            if (candNormal.TryGetValue(cell + t + t, out var n3) && n3 == n)
                continue;

            // measure thickness along n (how many wall rows until room)
            int depth = 0;
            for (int d = 1; d <= maxWallThickness; d++)
            {
                var mid = cell + n * d;
                if (!wallsRef.Contains(mid)) break;
                depth++;
                var after = cell + n * (d + 1);
                if (roomFloorsRef.Contains(after)) break;
            }
            if (depth <= 0) continue;

            openings.Add(new Opening {
                a = cell,
                b = cell + t,
                n = n,
                depth = depth,
                o = (n.x != 0) ? Orientation.Vertical : Orientation.Horizontal
            });

            seen.Add(cell);
            seen.Add(cell + t);
        }

        // --- Carve & refresh ---
        int cleared = 0;
        if (!detectOnly && openings.Count > 0)
        {
            foreach (var walls in wallMaps)
            {
                foreach (var o in openings)
                {
                    for (int d = 0; d < o.depth; d++)
                    {
                        var aLocal = RefToLocal(walls, o.a + o.n * d);
                        var bLocal = RefToLocal(walls, o.b + o.n * d);
                        if (walls.HasTile(aLocal)) { walls.SetTile(aLocal, null); cleared++; }
                        if (walls.HasTile(bLocal)) { walls.SetTile(bLocal, null); cleared++; }
                    }
                }
                walls.CompressBounds();
                walls.RefreshAllTiles(); // regular tilemaps refresh instantly; safe to call
            }

            if (floorDoorTile && roomFloorMaps.Count > 0)
            {
                var paint = roomFloorMaps[0];
                foreach (var o in openings)
                {
                    paint.SetTile(RefToLocal(paint, o.a), floorDoorTile);
                    paint.SetTile(RefToLocal(paint, o.b), floorDoorTile);
                }
                paint.CompressBounds();
                paint.RefreshAllTiles();
            }
        }

        Debug.Log($"[DoorPost] {(detectOnly ? "Detected" : "Carved")} {openings.Count} doorway(s); tiles cleared ~{cleared}.");
    }

    // ---- utils ----
    private static HashSet<Vector3Int> BuildRefSet(List<Tilemap> maps, Func<Tilemap, Vector3Int, Vector3Int> toRef)
    {
        var set = new HashSet<Vector3Int>();
        foreach (var tm in maps)
            foreach (var p in tm.cellBounds.allPositionsWithin)
                if (tm.HasTile(p)) set.Add(toRef(tm, p));
        return set;
    }
}