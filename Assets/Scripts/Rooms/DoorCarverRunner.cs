using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

[ExecuteAlways]
[DefaultExecutionOrder(1000)]
public class DoorCarverRunner : MonoBehaviour
{
    [Header("Search (case-insensitive substring match)")]
    [SerializeField] private string floorsNameContains = "floor";
    [SerializeField] private string wallsNameContains  = "walls";

    [Header("Doorway / Carve Settings")]
    [SerializeField] private TileBase floorDoorTile;
    [SerializeField, Min(1)] private int gapDepth = 1;
    [SerializeField] private bool detectExistingGaps = true;

    [Header("Door Prefab Placement")]
    [SerializeField] private bool spawnDoors = true;
    [SerializeField] private Door doorPrefab;
    [SerializeField] private Transform doorParent;

    [Header("Timing")]
    [SerializeField] private float timeoutSeconds = 5f;
    [SerializeField] private float delayAfterFound = 0f;
    [SerializeField] private bool verboseLogs = true;

    [Header("Navmesh / A*")]
    [SerializeField] private bool rescanAstar = true;

    private Coroutine routine;

    void OnEnable() => Arm();
    void OnValidate() { if (!Application.isPlaying) Arm(); }

    [ContextMenu("Run Now")] public void RunNow() => Arm(immediate: true);

    private void Arm(bool immediate = false)
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(RunWhenReady(immediate));
    }

    private IEnumerator RunWhenReady(bool immediate)
    {
        if (verboseLogs) Debug.Log("[DoorCarverRunner] Polling for generated Grid/Tilemaps...");
        if (!immediate) yield return null;

        float t0 = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - t0 < timeoutSeconds)
        {
            var grids = FindObjectsByType<Grid>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            Tilemap floors = null, walls = null;
            string fNeedle = floorsNameContains.ToLowerInvariant();
            string wNeedle = wallsNameContains.ToLowerInvariant();

            for (int g = 0; g < grids.Length; g++)
            {
                var maps = grids[g].GetComponentsInChildren<Tilemap>(true);
                floors = null; walls = null;

                foreach (var tm in maps)
                {
                    string n = tm.name.ToLowerInvariant();
                    if (floors == null && n.Contains(fNeedle)) floors = tm;
                    if (walls  == null && n.Contains(wNeedle))  walls  = tm;
                }

                if (floors != null && walls != null)
                {
                    // Wait until floors has at least one tile
                    bool hasTiles = false;
                    for (int i = 0; i < 60; i++)
                    {
                        if (CountTilesFast(floors) > 0) { hasTiles = true; break; }
                        yield return null;
                    }
                    if (!hasTiles && verboseLogs) Debug.LogWarning("[DoorCarverRunner] Floor tilemap stayed empty; retrying...");

                    if (delayAfterFound > 0f) yield return new WaitForSecondsRealtime(delayAfterFound);

                    var go = new GameObject("DoorCarver (temp)");
                    go.transform.SetParent(grids[g].transform, false);

                    var carver = go.AddComponent<DoorCarver>();
                    carver.floors = floors;
                    carver.walls  = walls;
                    carver.floorDoorTile = floorDoorTile;
                    carver.gapDepth = gapDepth;
                    carver.detectExistingGaps = detectExistingGaps;
                    carver.spawnDoors = spawnDoors;
                    carver.doorPrefab = doorPrefab;
                    carver.doorParent = doorParent;

                    carver.Carve();
                    TryRescanAstar();

#if UNITY_EDITOR
                    if (!Application.isPlaying) DestroyImmediate(go);
                    else Destroy(go);
#else
                    Destroy(go);
#endif
                    if (verboseLogs) Debug.Log("[DoorCarverRunner] Carve complete.");
                    yield break;
                }
            }
            yield return null;
        }

        Debug.LogWarning("[DoorCarverRunner] Timed out. Check tilemap names or layer structure.");
    }

    private static int CountTilesFast(Tilemap tm)
    {
        var b = tm.cellBounds;
        foreach (var p in b.allPositionsWithin)
            if (tm.HasTile(p)) return 1;
        return 0;
    }

    private void TryRescanAstar()
    {
        if (!rescanAstar) return;

        MonoBehaviour astar = null; System.Type astarType = null;
        var all = Resources.FindObjectsOfTypeAll<MonoBehaviour>();
        for (int i = 0; i < all.Length; i++)
        {
            var mb = all[i];
            if (!mb) continue;
            if (mb.gameObject.scene.IsValid() && mb.hideFlags == HideFlags.None)
            {
                var t = mb.GetType();
                if (t != null && t.FullName == "Pathfinding.AstarPath") { astar = mb; astarType = t; break; }
            }
        }

        if (astar != null && astarType != null)
        {
            var scan = astarType.GetMethod("Scan", System.Type.EmptyTypes);
            if (scan != null) scan.Invoke(astar, null);
            if (verboseLogs) Debug.Log("[DoorCarverRunner] A* rescanned after carving.");
        }
        else if (verboseLogs) Debug.Log("[DoorCarverRunner] No A* found; skipping rescan.");
    }
}