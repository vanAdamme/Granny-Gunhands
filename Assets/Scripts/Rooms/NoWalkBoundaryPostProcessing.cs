// Bakes a pixel-perfect "NoWalk" CompositeCollider2D around the Floor outline.
// Put this BEFORE CurrentRoomDetection and your A* rescan step in the post-process list.

#if UNITY_EDITOR || UNITY_2022_1_OR_NEWER
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Rooms.PostProcessing
{
    using Edgar.Unity;

    [CreateAssetMenu(menuName = "Edgar/PostProcess/NoWalk Boundary From Floor", fileName = "NoWalkBoundaryPostProcessing")]
    public class NoWalkBoundaryPostProcessing : DungeonGeneratorPostProcessingGrid2D
    {
        [Header("Source/Output")]
        [Tooltip("Name of the Tilemap that contains walkable floor tiles.")]
        public string floorTilemapName = "Floor";

        [Tooltip("Name of the GameObject created under the generated level root.")]
        public string outputObjectName = "NoWalkBoundary";

        [Tooltip("Child host under outputObjectName that actually carries Rigidbody/Colliders.")]
        public string colliderHostName = "NoWalkBoundary_ColliderHost";

        [Tooltip("Layer to assign to the produced boundary collider(s). Include this in your pathfinding Collision mask.")]
        public int noWalkLayer = 0; // e.g., LayerMask.NameToLayer("NoWalk")

        [Header("Collider Settings")]
        [Tooltip("Composite edge thickness in world units. ~nodeSize/2 is a good start for grid pathfinding.")]
        [Min(0f)] public float edgeRadius = 0.05f;

        [Tooltip("Logs each step of the bake for debugging.")]
        public bool verboseLogging = true;

        public override void Run(DungeonGeneratorLevelGrid2D level)
        {
            var root = level.RootGameObject;
            if (!root)
            {
                Debug.LogError("[NoWalkBoundary] RootGameObject missing.");
                return;
            }
            if (verboseLogging) Debug.Log("[NoWalkBoundary] Start");

            // 1) Find Floor tilemap (in entire generated subtree)
            var floor = FindChildByName<Tilemap>(root.transform, floorTilemapName);
            if (!floor)
            {
                Debug.LogError($"[NoWalkBoundary] Floor Tilemap '{floorTilemapName}' not found under generated level.");
                return;
            }
            if (verboseLogging) Debug.Log("[NoWalkBoundary] Found Floor tilemap");

            // 2) Create/clear output GO (container only)
            var output = GetOrCreateChild(root.transform, outputObjectName);
            output.layer = noWalkLayer;

            // 3) Create/clear a dedicated collider host as a child (so other systems touching the parent can't race us)
            var host = GetOrCreateChild(output.transform, colliderHostName);
            host.layer = noWalkLayer;

            // Hard reset host physics bits
            foreach (var c in host.GetComponents<Collider2D>()) Object.DestroyImmediate(c);
            var existingRb = host.GetComponent<Rigidbody2D>();
            if (existingRb) Object.DestroyImmediate(existingRb);
            if (verboseLogging) Debug.Log("[NoWalkBoundary] Host cleared");

            var rbOut = host.AddComponent<Rigidbody2D>();
            rbOut.bodyType = RigidbodyType2D.Static;

            var compositeOut = host.AddComponent<CompositeCollider2D>();
            compositeOut.geometryType   = CompositeCollider2D.GeometryType.Outlines;
            compositeOut.generationType = CompositeCollider2D.GenerationType.Manual;
            compositeOut.edgeRadius     = edgeRadius;
            compositeOut.useDelaunayMesh = false;

            if (verboseLogging) Debug.Log("[NoWalkBoundary] Host RB2D + Composite ready");

            // 4) Acquire a SOURCE composite safely:
            //    Prefer an existing parent Composite (common with "From Example").
            //    If none, create a temporary Composite on the FLOOR'S PARENT (not on Floor),
            //    and make the Floor contribute via a TilemapCollider2D.
            bool createdParentComposite = false;
            bool createdTilemapCollider = false;

            var parentForComposite = floor.transform.parent != null ? floor.transform.parent : root.transform;
            var sourceComposite = parentForComposite.GetComponent<CompositeCollider2D>();
            var sourceRb = parentForComposite.GetComponent<Rigidbody2D>();

            if (!sourceComposite)
            {
                if (verboseLogging) Debug.Log("[NoWalkBoundary] No parent Composite found; creating one on Floor's parent...");

                if (!sourceRb) sourceRb = parentForComposite.gameObject.AddComponent<Rigidbody2D>();
                sourceRb.bodyType = RigidbodyType2D.Static;

                sourceComposite = parentForComposite.gameObject.AddComponent<CompositeCollider2D>();
                sourceComposite.geometryType   = CompositeCollider2D.GeometryType.Outlines;
                sourceComposite.generationType = CompositeCollider2D.GenerationType.Synchronous;

                createdParentComposite = true;
            }

            var floorTmc = floor.GetComponent<TilemapCollider2D>();
            if (!floorTmc)
            {
                floorTmc = floor.gameObject.AddComponent<TilemapCollider2D>();
#if UNITY_2023_2_OR_NEWER
                floorTmc.compositeOperation = Collider2D.CompositeOperation.Merge;
#else
                floorTmc.usedByComposite = true;
#endif
                createdTilemapCollider = true;
            }
            else
            {
#if UNITY_2023_2_OR_NEWER
                floorTmc.compositeOperation = Collider2D.CompositeOperation.Merge;
#else
                floorTmc.usedByComposite = true;
#endif
            }

            Physics2D.SyncTransforms();

            if (verboseLogging)
                Debug.Log($"[NoWalkBoundary] Source composite at '{parentForComposite.name}' (paths={sourceComposite.pathCount})");

            // 5) Copy source paths into a Polygon feeding our output Composite
            var poly = host.AddComponent<PolygonCollider2D>();
#if UNITY_2023_2_OR_NEWER
            poly.compositeOperation = Collider2D.CompositeOperation.Merge;
#else
            poly.usedByComposite = true;    // Unity 2021/2022 fallback
#endif

            CopyCompositePaths(sourceComposite, parentForComposite, poly, host.transform);

            // 6) Bake geometry
            compositeOut.GenerateGeometry();
            if (verboseLogging)
                Debug.Log($"[NoWalkBoundary] Output composite baked (pathCount={compositeOut.pathCount})");

            // 7) Cleanup temporary bits only if we created them here
            if (createdTilemapCollider)
            {
                var tmc = floor.GetComponent<TilemapCollider2D>();
                if (tmc) Object.DestroyImmediate(tmc);
            }
            if (createdParentComposite)
            {
                var prb = parentForComposite.GetComponent<Rigidbody2D>();
                var pcc = parentForComposite.GetComponent<CompositeCollider2D>();
                if (pcc) Object.DestroyImmediate(pcc);
                if (prb) Object.DestroyImmediate(prb);
            }

#if ASTARPATH
            // Optional: rescan here if you do not use a separate post-process.
            if (AstarPath.active != null) AstarPath.active.Scan();
#endif

            if (verboseLogging) Debug.Log("[NoWalkBoundary] Done");
        }

        private static void CopyCompositePaths(CompositeCollider2D src, Transform srcSpace, PolygonCollider2D dstPoly, Transform dstSpace)
        {
            var count = src.pathCount;
            dstPoly.pathCount = count;

            var buf = new List<Vector2>(256);
            for (int i = 0; i < count; i++)
            {
                buf.Clear();
                buf.Capacity = Mathf.Max(buf.Capacity, src.GetPathPointCount(i));
                src.GetPath(i, buf);

                // Transform into output space
                for (int p = 0; p < buf.Count; p++)
                {
                    var wp = (Vector2)srcSpace.TransformPoint(buf[p]);
                    buf[p] = (Vector2)dstSpace.InverseTransformPoint(wp);
                }

                dstPoly.SetPath(i, buf);
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
    }
}
#endif
