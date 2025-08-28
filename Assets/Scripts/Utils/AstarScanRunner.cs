using System.Collections;
using UnityEngine;
using Pathfinding;

public class AstarScanRunner : MonoBehaviour
{
    public void Run(int framesToWait = 2)
    {
        StartCoroutine(DoScan(framesToWait));
    }

    private IEnumerator DoScan(int framesToWait)
    {
        // Wait a couple of frames so Tilemap/CompositeCollider2D finish building shapes
        for (int i = 0; i < Mathf.Max(1, framesToWait); i++)
            yield return null;

        Physics2D.SyncTransforms();

        if (AstarPath.active != null)
            AstarPath.active.Scan();
        else
            Debug.LogWarning("[A*] No active AstarPath found to Scan()");
    }
}