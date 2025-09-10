using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using Edgar.Unity;

namespace Rooms
{
    [CreateAssetMenu(menuName = "Edgar/PostProcess/CurrentRoomDetection", fileName = "CurrentRoomDetectionPostProcessing")]
    public class CurrentRoomDetectionPostProcessing : DungeonGeneratorPostProcessingGrid2D
    {
        [Header("Layers")]
        public string floorLayerName  = "Floor";
        public string noWalkLayerName = "NoWalk";

        [Header("Triggers")]
        [Tooltip("If true and NoWalkBoundary exists, room triggers mirror its outline; otherwise box fallback per room.")]
        public bool pixelPerfectTriggers = true;

        public override void Run(DungeonGeneratorLevelGrid2D level)
        {
            // Option B: do NOT build a global boundary here.
            var boundaryRoot = FindChildByName<Transform>(level.RootGameObject.transform, "NoWalkBoundary");
            CompositeCollider2D globalComposite = null;
            if (boundaryRoot != null)
            {
                globalComposite = boundaryRoot.GetComponentInChildren<CompositeCollider2D>(true);
            }

            if (pixelPerfectTriggers && globalComposite == null)
                Debug.LogWarning("[RoomDetect] NoWalkBoundary not found — room triggers will use box fallback.");

            foreach (var room in level.RoomInstances)
            {
                var rt = room.RoomTemplateInstance;

                // Any Floor tilemap in this room (only for box fallback sizing)
                int floorLayer = LayerMask.NameToLayer(floorLayerName);
                var anyFloor = RoomTemplateUtilsGrid2D.GetTilemaps(rt)
                               .FirstOrDefault(t => t && (t.gameObject.layer == floorLayer || t.name == "Floor"));

                var trigger = CreateRoomTrigger(rt.gameObject, anyFloor, globalComposite);

                var mgr = rt.GetComponent<CurrentRoomDetectionRoomManager>() ?? rt.AddComponent<CurrentRoomDetectionRoomManager>();
                mgr.RoomInstance = room;

                if (!trigger.GetComponent<CurrentRoomTriggerToSignals>())
                    trigger.AddComponent<CurrentRoomTriggerToSignals>();
            }
        }

        private static T FindChildByName<T>(Transform root, string name) where T : Component
        {
            var q = new Queue<Transform>();
            q.Enqueue(root);
            while (q.Count > 0)
            {
                var t = q.Dequeue();
                if (t.name == name)
                {
                    var c = t.GetComponent<T>();
                    if (c) return c;
                }
                for (int i = 0; i < t.childCount; i++) q.Enqueue(t.GetChild(i));
            }
            return null;
        }

        private GameObject CreateRoomTrigger(GameObject parent, Tilemap anyFloor, CompositeCollider2D globalComposite)
        {
            var go = new GameObject("RoomTrigger");
            go.transform.SetParent(parent.transform, false);

            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;

            if (pixelPerfectTriggers && globalComposite != null)
            {
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