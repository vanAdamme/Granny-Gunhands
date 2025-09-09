using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using Edgar.Unity;

namespace Rooms
{
    [CreateAssetMenu(menuName = "Rooms/Post-processing", fileName = "CurrentRoomDetectionPostProcessing")]
    public class CurrentRoomDetectionPostProcessing : DungeonGeneratorPostProcessingGrid2D
    {
        [Header("Layers")]
        public string floorLayerName  = "Floor";
        public string noWalkLayerName = "NoWalk";

        [Header("Triggers")]
        public bool pixelPerfectTriggers = true; // set true to mirror the global composite outline

        public override void Run(DungeonGeneratorLevelGrid2D level)
        {
            // 1) Build ONE global NoWalk composite at the level root
            var globalComposite = BuildGlobalNoWalkBoundary(level.RootGameObject, floorLayerName, noWalkLayerName);

            // 2) Per-room: create enter/leave triggers and wire the manager
            foreach (var room in level.RoomInstances)
            {
                var rt = room.RoomTemplateInstance;

                // Find any Floor tilemap in this room (for sizing fallback)
                int floorLayer = LayerMask.NameToLayer(floorLayerName);
                var anyFloor = RoomTemplateUtilsGrid2D.GetTilemaps(rt)
                               .FirstOrDefault(t => t && (t.gameObject.layer == floorLayer || t.name == "Floor"));

                var trigger = CreateRoomTrigger(rt.gameObject, anyFloor, globalComposite);
                var mgr = rt.GetComponent<CurrentRoomDetectionRoomManager>() ?? rt.AddComponent<CurrentRoomDetectionRoomManager>();
                mgr.RoomInstance = room;
                if (!trigger.GetComponent<CurrentRoomDetectionTriggerHandler>())
                    trigger.AddComponent<CurrentRoomDetectionTriggerHandler>();
            }
        }

   // helper
static Transform FindCommonAncestor(IList<Transform> nodes, Transform fallback)
{
    if (nodes == null || nodes.Count == 0) return fallback;
    Transform a = nodes[0];
    while (a != null)
    {
        bool ok = true;
        for (int i = 1; i < nodes.Count; i++)
            if (!nodes[i].IsChildOf(a)) { ok = false; break; }
        if (ok) return a;
        a = a.parent;
    }
    return fallback;
}

private CompositeCollider2D BuildGlobalNoWalkBoundary(GameObject levelRoot, string floorLayerName, string noWalkLayerName)
{
    int floorLayer = LayerMask.NameToLayer(floorLayerName);

    // collect ALL Floor tilemaps
    var floors = levelRoot.GetComponentsInChildren<UnityEngine.Tilemaps.Tilemap>(true)
                  .Where(t => t && (t.gameObject.layer == floorLayer || t.name == "Floor"))
                  .ToList();
    if (floors.Count == 0) { Debug.LogWarning("[NoWalk] No Floor tilemaps found."); return null; }

    // >>> mount on the common ancestor (likely "Generated Level/Rooms")
    var ancestor = FindCommonAncestor(floors.Select(f => f.transform).ToList(), levelRoot.transform);

    // get-or-add RB2D + Composite ON THE ANCESTOR
    var rb   = ancestor.GetComponent<Rigidbody2D>() ?? ancestor.gameObject.AddComponent<Rigidbody2D>();
    rb.bodyType = RigidbodyType2D.Static;

    var comp = ancestor.GetComponent<CompositeCollider2D>() ?? ancestor.gameObject.AddComponent<CompositeCollider2D>();
    comp.geometryType   = CompositeCollider2D.GeometryType.Outlines;
    comp.isTrigger      = false;
    comp.generationType = CompositeCollider2D.GenerationType.Synchronous;

    // put ancestor on NoWalk layer
    int noWalk = LayerMask.NameToLayer(noWalkLayerName);
    if (noWalk >= 0) ancestor.gameObject.layer = noWalk;

    // ensure each Floor has a TilemapCollider2D feeding the composite
    foreach (var floor in floors)
    {
        var tmc = floor.GetComponent<TilemapCollider2D>() ?? floor.gameObject.AddComponent<TilemapCollider2D>();
#if UNITY_2023_2_OR_NEWER
        tmc.compositeOperation = Collider2D.CompositeOperation.Merge;
#else
        tmc.usedByComposite = true;
#endif
    }

    Physics2D.SyncTransforms();
    return comp;
}



        private GameObject CreateRoomTrigger(GameObject parent, Tilemap anyFloor, CompositeCollider2D globalComposite)
        {
            var go = new GameObject("RoomTrigger");
            go.transform.SetParent(parent.transform, false);

            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;

            if (pixelPerfectTriggers && globalComposite != null)
            {
                // Clone the global composite outline into a PolygonCollider2D trigger
                var poly = go.AddComponent<PolygonCollider2D>();
                poly.isTrigger = true;

                int pathCount = globalComposite.pathCount;
                poly.pathCount = pathCount;

                var buf = new List<Vector2>(256);
                for (int i = 0; i < pathCount; i++)
                {
                    buf.Clear();
                    buf.Capacity = Mathf.Max(buf.Capacity, globalComposite.GetPathPointCount(i));
                    globalComposite.GetPath(i, buf);
                    poly.SetPath(i, buf.ToArray());
                }
            }
            else
            {
                // Cheap box fallback sized to this room's floor bounds
                var box = go.AddComponent<BoxCollider2D>();
                box.isTrigger = true;

                if (anyFloor)
                {
                    var b = anyFloor.localBounds;
                    box.offset = (Vector2)b.center;
                    box.size   = (Vector2)b.size;
                }
            }

            return go;
        }
    }
}
