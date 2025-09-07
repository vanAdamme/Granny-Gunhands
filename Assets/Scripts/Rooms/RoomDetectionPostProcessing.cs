using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using Edgar.Unity; // core runtime only

[CreateAssetMenu(menuName = "Rooms/Room Detection Post-processing", fileName = "RoomDetectionPostProcessing")]
public class RoomDetectionPostProcessing : DungeonGeneratorPostProcessingGrid2D
{
    // Configure this if your tilemap isn't literally called "Floor"
    [SerializeField] private string floorTilemapName = "Floor";

    public override void Run(DungeonGeneratorLevelGrid2D level)
    {
        foreach (var roomInstance in level.RoomInstances)
        {
            var root = roomInstance.RoomTemplateInstance;

            // 1) Ensure Floor trigger exists
            var floorTM = RoomTemplateUtilsGrid2D
                .GetTilemaps(root)
                .SingleOrDefault(x => x && x.name == floorTilemapName);

            if (floorTM == null)
            {
                Debug.LogWarning($"[RoomDetectionPostProcessing] Could not find a Tilemap named '{floorTilemapName}' under room '{root.name}'.");
                continue;
            }

            var floorGO = floorTM.gameObject;
            EnsureFloorTrigger(floorGO);

            // 2) Ensure our bridge exists on the room root
            var bridge = root.GetComponent<RoomEncounterBridge>() ?? root.AddComponent<RoomEncounterBridge>();

            // 3) Ensure a trigger handler exists on the Floor
			var handler = floorGO.GetComponent<RoomTriggerHandler>() ?? floorGO.AddComponent<RoomTriggerHandler>();
        }
    }

    private static void EnsureFloorTrigger(GameObject floor)
    {
        var tmCol = floor.GetComponent<TilemapCollider2D>() ?? floor.AddComponent<TilemapCollider2D>();
        #if UNITY_2023_2_OR_NEWER
        tmCol.compositeOperation = Collider2D.CompositeOperation.Merge;
        #else
        tmCol.usedByComposite = true;
        #endif

        var rb = floor.GetComponent<Rigidbody2D>() ?? floor.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;

        var composite = floor.GetComponent<CompositeCollider2D>() ?? floor.AddComponent<CompositeCollider2D>();
        composite.geometryType   = CompositeCollider2D.GeometryType.Polygons;
        composite.generationType = CompositeCollider2D.GenerationType.Manual;
        composite.isTrigger      = true;
    }
}
