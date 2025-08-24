using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Edgar.Unity;

[CreateAssetMenu(
    menuName = "Edgar/Post-processing/Spawn 2x1 Doors",
    fileName = "Spawn2x1DoorsPostProcessing")]
public class EdgarDoorSpawnerPostProcessing : DungeonGeneratorPostProcessingGrid2D
{
    [Header("Tilemap name needles (case-insensitive 'contains')")]
    [SerializeField] private string floorsNameContains = "floor";   // e.g., "ground"
    [SerializeField] private string wallsNameContains  = "walls";

    [Header("Door prefabs (2x1)")]
    [SerializeField] private Door verticalDoorPrefab;   // passage runs Up/Down (door spans Left–Right)
    [SerializeField] private Door horizontalDoorPrefab; // passage runs Left/Right (door spans Up–Down)
    [SerializeField] private Transform doorParentOverride; // optional parent; else uses generated Grid

    [Header("Behaviour")]
	[Tooltip("true = do NOT edit tiles; just place doors")]
    [SerializeField] private bool detectExistingGaps = true;
    [SerializeField, Min(1)] private int gapDepth = 1;       // when carving, how deep through thick walls
    [SerializeField] private TileBase floorDoorTile;         // optional: paint floor where we carve

	public override void Run(DungeonGeneratorLevelGrid2D level)
	{
		var root = level.RootGameObject != null ? level.RootGameObject.transform : null;
		var grid = root ? root.GetComponentInChildren<Grid>(true) : null;
		if (!grid) { Debug.LogWarning("[DoorPost] No Grid under generated level root."); return; }

		// Collect ALL floor/wall tilemaps
		string fNeedle = floorsNameContains.ToLowerInvariant();
		string wNeedle = wallsNameContains.ToLowerInvariant();
		var allTms = grid.GetComponentsInChildren<Tilemap>(true);

		var floorTms = new List<Tilemap>();
		var wallTms  = new List<Tilemap>();
		foreach (var tm in allTms)
		{
			var n = tm.name.ToLowerInvariant();
			if (n.Contains(fNeedle)) floorTms.Add(tm);
			if (n.Contains(wNeedle)) wallTms.Add(tm);
		}
		if (floorTms.Count == 0 || wallTms.Count == 0)
		{
			Debug.LogWarning($"[DoorPost] Could not find floor/walls tilemaps. Needles '{floorsNameContains}' / '{wallsNameContains}'.");
			return;
		}

		// Build unified occupancy sets
		var floorSet = new HashSet<Vector3Int>();
		var wallSet  = new HashSet<Vector3Int>();

		BoundsInt union = new BoundsInt(Vector3Int.zero, Vector3Int.zero);
		bool unionInit = false;

		foreach (var tm in floorTms)
		{
			var b = tm.cellBounds;
			if (!unionInit) { union = b; unionInit = true; } else union = Encapsulate(union, b);
			foreach (var p in b.allPositionsWithin) if (tm.HasTile(p)) floorSet.Add(p);
		}
		foreach (var tm in wallTms)
		{
			var b = tm.cellBounds;
			if (!unionInit) { union = b; unionInit = true; } else union = Encapsulate(union, b);
			foreach (var p in b.allPositionsWithin) if (tm.HasTile(p)) wallSet.Add(p);
		}

		// Detect 2×1 openings against unified view
		var openings = FindTwoWideOpeningsUnified(floorSet, wallSet, union);

		// Optional carve (remove the two wall tiles at each opening)
		if (openings.Count > 0)
		{
			if (!detectExistingGaps)
			{
				foreach (var o in openings)
				{
					ClearWallAt(o.a, wallTms);
					ClearWallAt(o.b, wallTms);
					// optional thickness (gapDepth)
					if (gapDepth > 1)
					{
						if (o.orientation == Orientation.Vertical)
						{
							for (int d = 1; d < gapDepth; d++)
							{ ClearWallAt(o.a + new Vector3Int(-d, 0, 0), wallTms);
							ClearWallAt(o.b + new Vector3Int(+d, 0, 0), wallTms); }
						}
						else
						{
							for (int d = 1; d < gapDepth; d++)
							{ ClearWallAt(o.a + new Vector3Int(0,-d, 0), wallTms);
							ClearWallAt(o.b + new Vector3Int(0,+d, 0), wallTms); }
						}
					}
				}
			}
			// Even if detectExistingGaps = true, you might want visuals gone:
			// uncomment next block if you always want wall sprites removed.
			/*
			else
			{
				foreach (var o in openings)
				{ ClearWallAt(o.a, wallTms); ClearWallAt(o.b, wallTms); }
			}
			*/

			if (floorDoorTile != null)
			{
				foreach (var o in openings)
				{
					PaintFloorAt(o.a, floorTms);
					PaintFloorAt(o.b, floorTms);
				}
			}

			// compress bounds on all changed layers
			foreach (var tm in wallTms) tm.CompressBounds();
			foreach (var tm in floorTms) tm.CompressBounds();
		}

		// Spawn doors
		if (openings.Count > 0)
			SpawnDoors(openings, wallTms[0], root); // use first wall TM for cell size & default parent

		Debug.Log($"[DoorPost] {(detectExistingGaps ? "Detected" : "Carved")} {openings.Count} two‑wide openings and spawned doors.");

		// ---------- local helpers ----------
		static BoundsInt Encapsulate(BoundsInt a, BoundsInt b)
		{
			var min = Vector3Int.Min(a.min, b.min);
			var max = Vector3Int.Max(a.max, b.max);
			return new BoundsInt(min, max - min);
		}

		void ClearWallAt(Vector3Int cell, List<Tilemap> wallMaps)
		{
			for (int i = 0; i < wallMaps.Count; i++)
			{
				var tm = wallMaps[i];
				if (tm.HasTile(cell)) { tm.SetTile(cell, null); return; }
			}
		}

		void PaintFloorAt(Vector3Int cell, List<Tilemap> floorMaps)
		{
			// Prefer an existing floor owner; else fall back to first floor tilemap
			foreach (var tm in floorMaps)
				if (tm.HasTile(cell)) { tm.SetTile(cell, floorDoorTile); return; }
			floorMaps[0].SetTile(cell, floorDoorTile);
		}
	}

    /* ---------- detection ---------- */
	private enum Orientation { Vertical, Horizontal }
	private struct Opening { public Vector3Int a, b; public Orientation orientation; }

	private List<Opening> FindTwoWideOpeningsUnified(HashSet<Vector3Int> floor, HashSet<Vector3Int> wall, BoundsInt scanBounds)
	{
		var result = new List<Opening>(64);
		var visited = new HashSet<Vector3Int>();
		Vector3Int L = new(-1,0,0), R = new(1,0,0), U = new(0,1,0), D = new(0,-1,0);

		foreach (var p in scanBounds.allPositionsWithin)
		{
			if (visited.Contains(p)) continue;

			// Vertical passage: two adjacent walls p & p+R; floor above both and below both
			if (wall.Contains(p) && wall.Contains(p + R))
			{
				bool above = floor.Contains(p + U) && floor.Contains(p + R + U);
				bool below = floor.Contains(p + D) && floor.Contains(p + R + D);
				if (above && below)
				{
					bool leftPair = wall.Contains(p + L) && wall.Contains(p)
								&& floor.Contains(p + L + U) && floor.Contains(p + U)
								&& floor.Contains(p + L + D) && floor.Contains(p + D);
					if (!leftPair)
					{
						result.Add(new Opening { a = p, b = p + R, orientation = Orientation.Vertical });
						visited.Add(p); visited.Add(p + R);
						continue;
					}
				}
			}

			// Horizontal passage: two adjacent walls p & p+U; floor left & right of both
			if (wall.Contains(p) && wall.Contains(p + U))
			{
				bool left  = floor.Contains(p + L) && floor.Contains(p + U + L);
				bool right = floor.Contains(p + R) && floor.Contains(p + U + R);
				if (left && right)
				{
					bool downPair = wall.Contains(p + D) && wall.Contains(p)
								&& floor.Contains(p + D + L) && floor.Contains(p + L)
								&& floor.Contains(p + D + R) && floor.Contains(p + R);
					if (!downPair)
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

    /* ---------- carving (optional) ---------- */
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
                    {
                        TryClear(walls, o.a + new Vector3Int(-d,0,0));
                        TryClear(walls, o.b + new Vector3Int(+d,0,0));
                    }
                }
                else
                {
                    for (int d = 1; d < gapDepth; d++)
                    {
                        TryClear(walls, o.a + new Vector3Int(0,-d,0));
                        TryClear(walls, o.b + new Vector3Int(0,+d,0));
                    }
                }
            }
        }

        static void TryClear(Tilemap walls, Vector3Int c) { if (walls.HasTile(c)) walls.SetTile(c, null); }
    }

    /* ---------- spawning ---------- */
	private void SpawnDoors(List<Opening> openings, Tilemap anyWallForSizes, Transform levelRoot)
	{
		var parent = doorParentOverride 
			? doorParentOverride 
			: EnsureChild(levelRoot, "Doors"); // create "Doors" once

		var cellSize = anyWallForSizes && anyWallForSizes.layoutGrid
			? anyWallForSizes.layoutGrid.cellSize
			: Vector3.one;

		foreach (var o in openings)
		{
			var aCenter = anyWallForSizes.GetCellCenterWorld(o.a);
			var bCenter = anyWallForSizes.GetCellCenterWorld(o.b);
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
		}

		static Transform EnsureChild(Transform root, string name)
		{
			if (!root) return null;
			var t = root.Find(name);
			if (t) return t;
			var go = new GameObject(name);
			go.transform.SetParent(root, false);
			return go.transform;
		}
	}
}