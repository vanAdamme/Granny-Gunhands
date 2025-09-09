using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using Edgar.Unity;

namespace Rooms
{
    [CreateAssetMenu(menuName = "Rooms/Post-processing", fileName = "CurrentRoomDetectionPostProcessing")]

    #region codeBlock:2d_currentRoomDetection_postProcessing

    public class CurrentRoomDetectionPostProcessing : DungeonGeneratorPostProcessingGrid2D
    {
        public override void Run(DungeonGeneratorLevelGrid2D level)
        {
            foreach (var roomInstance in level.RoomInstances)
            {
                var roomTemplateInstance = roomInstance.RoomTemplateInstance;

                // Find floor tilemap layer
                var tilemaps = RoomTemplateUtilsGrid2D.GetTilemaps(roomTemplateInstance);
                var floor = tilemaps.Single(x => x.name == "Floor").gameObject;

                ConfigureNoWalkBoundary(floor);
                var triggerGO = CreateRoomTrigger(roomTemplateInstance.gameObject, floor.GetComponent<Tilemap>());

                // Add the room manager component
                var roomManager = roomTemplateInstance.AddComponent<CurrentRoomDetectionRoomManager>();
                roomManager.RoomInstance = roomInstance;
                triggerGO.AddComponent<CurrentRoomDetectionTriggerHandler>();

                // Add current room detection handler
                floor.AddComponent<CurrentRoomDetectionTriggerHandler>();
            }
        }

        private void ConfigureNoWalkBoundary(GameObject floorGO)
        {
            // Ensure required components
            var rb = floorGO.GetComponent<Rigidbody2D>() ?? floorGO.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;

            var tmc = floorGO.GetComponent<TilemapCollider2D>() ?? floorGO.AddComponent<TilemapCollider2D>();
#if UNITY_2023_2_OR_NEWER
    tmc.compositeOperation = Collider2D.CompositeOperation.Merge;
#else
    tmc.usedByComposite = true;
#endif

            var comp = floorGO.GetComponent<CompositeCollider2D>() ?? floorGO.AddComponent<CompositeCollider2D>();
            comp.geometryType   = CompositeCollider2D.GeometryType.Outlines; // <= follow irregular room edge
            comp.isTrigger      = false;                                      // <= solid for PlayerFeet only
            comp.generationType = CompositeCollider2D.GenerationType.Manual;  // (optional) force rebuild
            comp.GenerateGeometry();

            // Put Floor on the NoWalk layer so only PlayerFeet collides (per your layer matrix)
            int noWalk = LayerMask.NameToLayer("NoWalk");
            if (noWalk >= 0) floorGO.layer = noWalk;
        }

        private GameObject CreateRoomTrigger(GameObject parent, Tilemap floor)
        {
            var go = new GameObject("RoomTrigger");
            go.transform.SetParent(parent.transform, false);

            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;

            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;

            // Size to the painted floor area (irregular rooms supported)
            var b = floor.localBounds;
            box.offset = (Vector2)b.center;
            box.size   = (Vector2)b.size;

            return go;
        }

        private void AddFloorCollider(GameObject floor)
        {
            var tilemapCollider2D = floor.AddComponent<TilemapCollider2D>();
#if UNITY_2023_2_OR_NEWER
        tilemapCollider2D.compositeOperation = Collider2D.CompositeOperation.Merge;
#else
        tilemapCollider2D.usedByComposite = true;
#endif

            var compositeCollider2d = floor.AddComponent<CompositeCollider2D>();
            compositeCollider2d.geometryType = CompositeCollider2D.GeometryType.Polygons;
            compositeCollider2d.isTrigger = true;
            compositeCollider2d.generationType = CompositeCollider2D.GenerationType.Manual;

            floor.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
        }
    }
    #endregion
}