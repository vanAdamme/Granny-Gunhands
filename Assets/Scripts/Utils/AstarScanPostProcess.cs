using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;
using Pathfinding;
using Edgar.Unity;

[CreateAssetMenu(menuName = "Edgar/PostProcess/A* Rescan (Debug)", fileName = "DungeonAstarPostProcess")]
public class DungeonAstarPostProcess : DungeonGeneratorPostProcessingGrid2D
{
    [SerializeField] private int extraFramesToWait = 1;
    [SerializeField] private bool resizeGridToLevel = true;
    [SerializeField] private int borderNodes = 2;

    public override void Run(DungeonGeneratorLevelGrid2D level)
    {
        var runner = level.RootGameObject.GetComponent<_AstarScanRunner>();
        if (!runner) runner = level.RootGameObject.AddComponent<_AstarScanRunner>();

        runner.Run(extraFramesToWait, resizeGridToLevel, borderNodes);
    }

    private class _AstarScanRunner : MonoBehaviour
    {
        public void Run(int framesToWait, bool resize, int border)
        {
            StartCoroutine(DoScan(framesToWait, resize, border));
        }

        private IEnumerator DoScan(int framesToWait, bool resize, int border)
        {
            yield return null;
            for (int i = 0; i < framesToWait; i++) yield return null;

            var astar = AstarPath.active;
            var gg = astar ? astar.data?.gridGraph : null;

            if (gg == null)
            {
                Debug.LogWarning("[A*Runner] No GridGraph found on AstarPath.");
                yield break;
            }

            if (resize)
            {
                var tms = GetComponentsInChildren<Tilemap>(true);

                if (tms.Length > 0)
                {
                    var bounds = new Bounds();
                    bool hasBounds = false;

                    foreach (var tm in tms)
                    {
                        if (tm.localBounds.size.sqrMagnitude < 0.001f) continue;
                        var wb = TransformBounds(tm.transform.localToWorldMatrix, tm.localBounds);
                        if (!hasBounds) { bounds = wb; hasBounds = true; }
                        else bounds.Encapsulate(wb);
                    }

                    if (hasBounds)
                    {
                        bounds.Expand(border * gg.nodeSize * 2f);
                        gg.center = bounds.center;
                        int newW = Mathf.CeilToInt(bounds.size.x / gg.nodeSize);
                        int newD = Mathf.CeilToInt(bounds.size.y / gg.nodeSize);
                        gg.SetDimensions(newW, newD, gg.nodeSize);
                    }
                    else
                    {
                        Debug.LogWarning("[A*Runner] No usable bounds found in tilemaps!");
                    }
                }
            }

            Physics2D.SyncTransforms();
            astar.Scan();
        }

        private static Bounds TransformBounds(Matrix4x4 m, Bounds b)
        {
            var c = m.MultiplyPoint3x4(b.center);
            var e = b.extents;
            var ax = m.MultiplyVector(new Vector3(e.x, 0, 0));
            var ay = m.MultiplyVector(new Vector3(0, e.y, 0));
            var az = m.MultiplyVector(new Vector3(0, 0, e.z));
            var size = new Vector3(
                Mathf.Abs(ax.x) + Mathf.Abs(ay.x) + Mathf.Abs(az.x),
                Mathf.Abs(ax.y) + Mathf.Abs(ay.y) + Mathf.Abs(az.y),
                Mathf.Abs(ax.z) + Mathf.Abs(ay.z) + Mathf.Abs(az.z)
            ) * 2f;
            return new Bounds(c, size);
        }
    }
}