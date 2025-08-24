using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[ExecuteAlways]
public class DoorCarver : MonoBehaviour
{
    [Header("Tilemaps (same Grid)")]
    public Tilemap floors;
    public Tilemap walls;
    public TileBase floorDoorTile;

    [Header("Doorway Settings")]
    [Min(1)] public int gapDepth = 1;
    public bool detectExistingGaps = false;

    [Header("Spawn Door Prefab")]
    public bool spawnDoors = true;
    public Door doorPrefab;
    public Transform doorParent;

    enum Orientation { Vertical, Horizontal }
    struct Opening { public Vector3Int a, b; public Orientation orientation; }

    [ContextMenu("Carve & Spawn Doors")]
    public void Carve()
    {
        if (!floors || !walls) { Debug.LogError("[DoorCarver] Assign floors & walls."); return; }

        var openings = FindTwoWideOpenings();

        if (!detectExistingGaps) CarveOpenings(openings);

        if (!detectExistingGaps && floorDoorTile)
        {
            foreach (var o in openings) { floors.SetTile(o.a, floorDoorTile); floors.SetTile(o.b, floorDoorTile); }
        }

        if (!detectExistingGaps) { walls.CompressBounds(); floors.CompressBounds(); }

        if (spawnDoors && doorPrefab != null && openings.Count > 0) SpawnDoors(openings);

        Debug.Log($"[DoorCarver] {(detectExistingGaps ? "Detected" : "Carved")} {openings.Count} two‑wide openings.");
    }

    List<Opening> FindTwoWideOpenings()
    {
        var openings = new List<Opening>(64);
        var floorSet = new HashSet<Vector3Int>();
        foreach (var p in floors.cellBounds.allPositionsWithin) if (floors.HasTile(p)) floorSet.Add(p);

        var visited = new HashSet<Vector3Int>();
        Vector3Int L = new(-1,0,0), R = new(1,0,0), U = new(0,1,0), D = new(0,-1,0);

        foreach (var p in walls.cellBounds.allPositionsWithin)
        {
            if (visited.Contains(p)) continue;

            if (walls.HasTile(p) && walls.HasTile(p + R))
            {
                bool above = floorSet.Contains(p + U) && floorSet.Contains(p + R + U);
                bool below = floorSet.Contains(p + D) && floorSet.Contains(p + R + D);
                if (above && below)
                {
                    bool leftAlsoPair = walls.HasTile(p + L) && walls.HasTile(p) &&
                                        floorSet.Contains(p + L + U) && floorSet.Contains(p + U) &&
                                        floorSet.Contains(p + L + D) && floorSet.Contains(p + D);
                    if (!leftAlsoPair)
                    {
                        openings.Add(new Opening { a = p, b = p + R, orientation = Orientation.Vertical });
                        visited.Add(p); visited.Add(p + R);
                        continue;
                    }
                }
            }

            if (walls.HasTile(p) && walls.HasTile(p + U))
            {
                bool left  = floorSet.Contains(p + L) && floorSet.Contains(p + U + L);
                bool right = floorSet.Contains(p + R) && floorSet.Contains(p + U + R);
                if (left && right)
                {
                    bool downAlsoPair = walls.HasTile(p + D) && walls.HasTile(p) &&
                                        floorSet.Contains(p + D + L) && floorSet.Contains(p + L) &&
                                        floorSet.Contains(p + D + R) && floorSet.Contains(p + R);
                    if (!downAlsoPair)
                    {
                        openings.Add(new Opening { a = p, b = p + U, orientation = Orientation.Horizontal });
                        visited.Add(p); visited.Add(p + U);
                        continue;
                    }
                }
            }
        }

        return openings;
    }

    void CarveOpenings(List<Opening> openings)
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
                    { TryClear(o.a + new Vector3Int(-d,0,0)); TryClear(o.b + new Vector3Int(+d,0,0)); }
                }
                else
                {
                    for (int d = 1; d < gapDepth; d++)
                    { TryClear(o.a + new Vector3Int(0,-d,0)); TryClear(o.b + new Vector3Int(0,+d,0)); }
                }
            }
        }

        void TryClear(Vector3Int c) { if (walls.HasTile(c)) walls.SetTile(c, null); }
    }

    void SpawnDoors(List<Opening> openings)
    {
        var parent = doorParent ? doorParent : (walls ? walls.transform : transform);
        var cellSize = (walls && walls.layoutGrid) ? walls.layoutGrid.cellSize : Vector3.one;

        foreach (var o in openings)
        {
            var aCenter = walls.GetCellCenterWorld(o.a);
            var bCenter = walls.GetCellCenterWorld(o.b);
            var center  = (aCenter + bCenter) * 0.5f;

            var door = Instantiate(doorPrefab, center, Quaternion.identity, parent);

            if (!door.TryGetComponent<BoxCollider2D>(out var col))
                col = door.gameObject.AddComponent<BoxCollider2D>();

            const float t = 0.9f;
            if (o.orientation == Orientation.Vertical)
                col.size = new Vector2(cellSize.x * 2f, cellSize.y * t);
            else
                col.size = new Vector2(cellSize.x * t, cellSize.y * 2f);

            door.name = $"Door {o.orientation} ({o.a.x},{o.a.y})-({o.b.x},{o.b.y})";
        }
    }
}