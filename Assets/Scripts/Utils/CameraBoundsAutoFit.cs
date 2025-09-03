using UnityEngine;
using Unity.Cinemachine;

[ExecuteAlways, RequireComponent(typeof(BoxCollider2D))]
public class CameraBoundsAutoFit : MonoBehaviour
{
    [SerializeField] Camera targetCamera;             // if null, uses Camera.main
    [SerializeField] float padding = 0.5f;            // world units padding around the minimum
    [SerializeField] bool expandOnly = true;          // only grow; never shrink (avoid level cuts)

    BoxCollider2D box;

    void OnEnable()   { box = GetComponent<BoxCollider2D>(); box.isTrigger = true; SyncNow(); }
    void OnValidate() { box = GetComponent<BoxCollider2D>(); if (isActiveAndEnabled) SyncNow(); }
    void LateUpdate() { SyncNow(); }                   // keeps up with window/PPU/aspect changes

    void SyncNow()
    {
        var cam = targetCamera ? targetCamera : Camera.main;
        if (!cam || !box || !cam.orthographic) return;

        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;
        Vector2 min = new Vector2(halfW * 2f, halfH * 2f) + Vector2.one * padding;

        var size = box.size;
        if (expandOnly)
            size = new Vector2(Mathf.Max(size.x, min.x), Mathf.Max(size.y, min.y));
        else
            size = min;

        if (box.size != size) {
            box.size = size;
            // Invalidate any CinemachineConfiner2D using this collider
            foreach (var c in FindObjectsOfType<CinemachineConfiner2D>(true))
                if (c.BoundingShape2D == box) c.InvalidateBoundingShapeCache();
        }
    }
}