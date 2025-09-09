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
        public string floorLayerName = "Floor";   // tilemaps painted as walkable floor
        public string noWalkLayerName = "NoWalk"; // boundary collider layer

        public override void Run(DungeonGeneratorLevelGrid2D level)
        {
            foreach (var room in level.RoomInstances)
            {
                var rt = room.RoomTemplateInstance;

                // Collect all tilemaps that are on the Floor layer (supports multiple floor tilemaps)
                int floorLayer = LayerMask.NameToLayer(floorLayerName);
                var tilemaps = RoomTemplateUtilsGrid2D.GetTilemaps(rt)
                              .Where(t => t.gameObject.layer == floorLayer || t.name == "Floor")
                              .ToList();

                // 1) Build a solid NoWalk boundary that hugs Floor outline(s)
                BuildNoWalkBoundary(rt.gameObject, tilemaps);

                // 2) Build a separate trigger for room enter/exit
                var trigger = CreateRoomTrigger(rt.gameObject, tilemaps.FirstOrDefault());
                var mgr = rt.AddComponent<CurrentRoomDetectionRoomManager>();
                mgr.RoomInstance = room;
                trigger.AddComponent<CurrentRoomDetectionTriggerHandler>();
            }
        }

        private void BuildNoWalkBoundary(GameObject rtRoot, System.Collections.Generic.List<Tilemap> floors)
        {
            if (floors.Count == 0) return;

            // Parent that owns the Rigidbody2D + CompositeCollider2D
            var boundaryGO = new GameObject("FloorBoundary");
            boundaryGO.transform.SetParent(rtRoot.transform, false);
            boundaryGO.layer = LayerMask.NameToLayer(noWalkLayerName);

            var rb = boundaryGO.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;

            var composite = boundaryGO.AddComponent<CompositeCollider2D>();
            composite.geometryType = CompositeCollider2D.GeometryType.Outlines; // follow floor edge
            composite.isTrigger = false;
            composite.generationType = CompositeCollider2D.GenerationType.Manual;

            // Feed child TilemapCollider2D(s) to the composite via usedByComposite
            foreach (var floor in floors)
            {
                var tmCol = floor.GetComponent<TilemapCollider2D>() ?? floor.gameObject.AddComponent<TilemapCollider2D>();
#if UNITY_2023_2_OR_NEWER
                tmCol.compositeOperation = Collider2D.CompositeOperation.Merge;
#else
                tmCol.usedByComposite = true;
#endif
                // Ensure these colliders attach to the parent's Rigidbody2D
                // (2D colliders on children attach to closest ancestor Rigidbody2D.)
                floor.transform.SetParent(boundaryGO.transform, true);
            }

            // Generate the unified edge
            composite.GenerateGeometry();
        }

        private GameObject CreateRoomTrigger(GameObject rtRoot, Tilemap anyFloor)
        {
            var triggerGO = new GameObject("RoomTrigger");
            triggerGO.transform.SetParent(rtRoot.transform, false);

            var rb = triggerGO.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;

            var box = triggerGO.AddComponent<BoxCollider2D>();
            box.isTrigger = true;

            // Size to floor bounds (works for irregular rooms)
            if (anyFloor != null)
            {
                var b = anyFloor.localBounds;
                box.offset = (Vector2)b.center;
                box.size   = (Vector2)b.size;
            }

            return triggerGO;
        }
    }
}