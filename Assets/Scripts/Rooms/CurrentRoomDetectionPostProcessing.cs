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

        public override void Run(DungeonGeneratorLevelGrid2D level)
        {
            foreach (var room in level.RoomInstances)
            {
                var rt = room.RoomTemplateInstance;

                // Collect all tilemaps that represent walkable floor
                int floorLayer = LayerMask.NameToLayer(floorLayerName);
                var floors = RoomTemplateUtilsGrid2D.GetTilemaps(rt)
                              .Where(t => t && (t.gameObject.layer == floorLayer || t.name == "Floor"))
                              .ToList();
                if (floors.Count == 0) continue;

                // Find a stable parent (usually the "Tilemaps" GO that owns Grid)
                var tilemapsParent = floors[0].transform.parent ? floors[0].transform.parent.gameObject : rt.gameObject;

                // 1) Ensure a composite on the parent (no reparenting of Floor)
                var rb = tilemapsParent.GetComponent<Rigidbody2D>() ?? tilemapsParent.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Static;

                var composite = tilemapsParent.GetComponent<CompositeCollider2D>() ?? tilemapsParent.AddComponent<CompositeCollider2D>();
                composite.geometryType = CompositeCollider2D.GeometryType.Outlines;
                composite.isTrigger = false;
                composite.generationType = CompositeCollider2D.GenerationType.Synchronous;

                // NoWalk layer lives on the composite parent so feet/ground enemies hit it
                int noWalk = LayerMask.NameToLayer(noWalkLayerName);
                if (noWalk >= 0 && tilemapsParent.layer != noWalk) tilemapsParent.layer = noWalk;

                // 2) Make each floor tilemap feed the composite (no reparenting)
                foreach (var floor in floors)
                {
                    var tmc = floor.GetComponent<TilemapCollider2D>();
                    if (!tmc) tmc = floor.gameObject.AddComponent<TilemapCollider2D>();

#if UNITY_2023_2_OR_NEWER
                    tmc.compositeOperation = Collider2D.CompositeOperation.Merge;
#else
                    tmc.usedByComposite = true;
#endif
                    // Make sure your tiles/rule tiles have Collider Type != None
                    // (Otherwise this collider stays empty.)
                }

                // 3) Room trigger (use your polygon copier here if you want pixel-perfect)
                var trigger = CreateRoomTrigger(rt.gameObject, floors.FirstOrDefault());
                var mgr = rt.AddComponent<CurrentRoomDetectionRoomManager>();
                mgr.RoomInstance = room;
                trigger.AddComponent<CurrentRoomDetectionTriggerHandler>();
            }
        }

        private GameObject CreateRoomTrigger(GameObject parent, Tilemap anyFloor)
        {
            var go = new GameObject("RoomTrigger");
            go.transform.SetParent(parent.transform, false);

            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;

            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;

            if (anyFloor != null)
            {
                var b = anyFloor.localBounds;
                box.offset = (Vector2)b.center;
                box.size   = (Vector2)b.size;
            }
            return go;
        }
    }
}
