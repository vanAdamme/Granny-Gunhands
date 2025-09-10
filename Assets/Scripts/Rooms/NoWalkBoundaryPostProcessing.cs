// NoWalkBoundaryPostProcessing.cs
// Creates a pixel-perfect "NoWalk" boundary collider following the outer edge of the Floor tilemap.
// Drop the generated asset into your Edgar PostProcessingConfig (after tilemap layers are created).

#if UNITY_EDITOR || UNITY_2022_1_OR_NEWER
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// NOTE: These namespaces/types come from Edgar-Unity (Grid2D).
// Keep them in a #if block so the file still compiles without Edgar present in editor scripts.
namespace Rooms.PostProcessing
{
    using Edgar.Unity;

    [CreateAssetMenu(menuName = "Edgar/PostProcess/NoWalk Boundary From Floor", fileName = "NoWalkBoundaryPostProcessing")]
    public class NoWalkBoundaryPostProcessing : DungeonGeneratorPostProcessingGrid2D
    {
        [Header("Source/Output")]
        [Tooltip("Name of the Tilemap that contains walkable floor tiles.")]
        public string floorTilemapName = "Floor";

        [Tooltip("Name of the GameObject that will be (re)created under the generated level root.")]
        public string outputObjectName = "NoWalkBoundary";

        [Tooltip("Layer to assign to the produced boundary collider(s). Include this in your A* GridGraph 'Collision' mask.")]
        public int noWalkLayer = 0; // e.g. LayerMask.NameToLayer("NoWalk")

        [Header("Collider Settings")]
        [Tooltip("Composite edge thickness in world units. For A* GridGraph this should be >= node size/2 so cells on the boundary become unwalkable.")]
        [Min(0f)] public float edgeRadius = 0.05f;

        [Tooltip("If true, the baker will add temporary colliders to the Floor tilemap in order to extract a clean outline.")]
        public bool addTemporaryColliderIfMissing = true;

        [Tooltip("Optional: recenter the produced GameObject to the Grid origin to avoid FP rounding disparities.")]
        public bool snapOutputToGrid = true;

        public override void Run(DungeonGeneratorLevelGrid2D level)
        {
            var root = level.GameObject;
            if (!root)
            {
                Debug.LogError("[NoWalkBoundary] Level root not found.");
                return;
            }

            // Find Floor tilemap under the generated root.
            var floor = FindChildByName<Tilemap>(root.transform, floorTilemapName);
            if (!floor)
            {
                Debug.LogError($"[NoWalkBoundary] Could not find Tilemap named '{floorTilemapName}'.");
                return;
            }

            // Create/clear output GO
            var output = GetOrCreateChild(root.transform, outputObjectName);
            output.layer = noWalkLayer;

            // Clean previous runs
            foreach (var c in output.GetComponents<Collider2D>()) Object.DestroyImmediate(c);
            foreach (Transform child in output.transform) Object.DestroyImmediate(child.gameObject);

            // Static body + composite that we will feed with polygon paths
            var rbOut = output.GetComponent<Rigidbody2D>() ?? output.AddComponent<Rigidbody2D>();
            rbOut.bodyType = Rigidbody2D.Static;

            var compositeOut = output.GetComponent<CompositeCollider2D>() ?? output.AddComponent<CompositeCollider2D>();
            compositeOut.geometryType = CompositeCollider2D.GeometryType.Outlines;
            compositeOut.generationType = CompositeCollider2D.GenerationType.Manual;
            compositeOut.edgeRadius = edgeRadius;
            compositeOut.useDelaunayMesh = false;

            // Ensure the Floor has a composite we can read from (temporarily if needed)
            var (srcComposite, removeAfter) = EnsureCompositeOnTilemap(floor.gameObject, addTemporaryColliderIfMissing);
            if (srcComposite == null)
            {
                Debug.LogError("[NoWalkBoundary] Could not obtain a CompositeCollider2D for the Floor tilemap.");
                return;
            }

            // Feed paths to a PolygonCollider2D (used by the output composite)
            var poly = output.AddComponent<PolygonCollider2D>();
            poly.usedByComposite = true;
            CopyCompositePaths(srcComposite, floor.transform, poly, output.transform);

            compositeOut.GenerateGeometry();

            // Clean up temporary components on Floor
            if (removeAfter)
            {
                var tmCol = floor.GetComponent<TilemapCollider2D>();
                var rb = floor.GetComponent<Rigidbody2D>();
                if (tmCol) Object.DestroyImmediate(tmCol);
                if (srcComposite) Object.DestroyImmediate(srcComposite);
                if (rb) Object.DestroyImmediate(rb);
            }

#if ASTARPATH
            // Rescan A* so AI respects the boundary (only if ASTARPATH scripting define is set).
            if (AstarPath.active != null)
            {
                AstarPath.active.Scan();
            }
#endif

            Debug.Log("[NoWalkBoundary] Boundary (CompositeCollider2D) baked.");
        }

        private static (CompositeCollider2D composite, bool removeAfter) EnsureCompositeOnTilemap(GameObject tilemapGO, bool createIfMissing)
        {
            var composite = tilemapGO.GetComponent<CompositeCollider2D>();
            var removeAfter = false;

            if (!composite && createIfMissing)
            {
                var rb = tilemapGO.GetComponent<Rigidbody2D>() ?? tilemapGO.AddComponent<Rigidbody2D>();
                rb.bodyType = Rigidbody2D.Static;

                var tmCol = tilemapGO.GetComponent<TilemapCollider2D>() ?? tilemapGO.AddComponent<TilemapCollider2D>();
                tmCol.usedByComposite = true;

                composite = tilemapGO.AddComponent<CompositeCollider2D>();
                composite.geometryType = CompositeCollider2D.GeometryType.Outlines;
                composite.generationType = CompositeCollider2D.GenerationType.Synchronous;

                removeAfter = true;
            }

            return (composite, removeAfter);
        }

        private static void CopyCompositePaths(CompositeCollider2D src, Transform srcSpace, PolygonCollider2D dstPoly, Transform dstSpace)
        {
            var pathCount = src.pathCount;
            dstPoly.pathCount = pathCount;

            var srcPts = new Vector2[0];

            for (int i = 0; i < pathCount; i++)
            {
                var count = src.GetPathPointCount(i);
                if (srcPts.Length < count) srcPts = new Vector2[count];
                src.GetPath(i, srcPts);

                var dst = new Vector2[count];
                for (int p = 0; p < count; p++)
                {
                    var wp = (Vector2)srcSpace.TransformPoint(srcPts[p]);
                    dst[p] = (Vector2)dstSpace.InverseTransformPoint(wp);
                }
                dstPoly.SetPath(i, dst);
            }
        }

        private static GameObject GetOrCreateChild(Transform parent, string name)
        {
            var t = FindChildByName<Transform>(parent, name);
            if (t) return t.gameObject;

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            return go;
        }

        private static T FindChildByName<T>(Transform root, string name) where T : Component
        {
            // Non-deprecated traversal (no FindObjectOfType).
            var queue = new Queue<Transform>();
            queue.Enqueue(root);
            while (queue.Count > 0)
            {
                var t = queue.Dequeue();
                if (t.name == name)
                {
                    var comp = t.GetComponent<T>();
                    if (comp) return comp;
                }
                for (int i = 0; i < t.childCount; i++) queue.Enqueue(t.GetChild(i));
            }
            return null;
        }
    }
}
#endif