using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class CanvasBoundsToCollider : MonoBehaviour
{
    [SerializeField] private RectTransform canvasRect;

    void Reset()
    {
        if (!canvasRect) canvasRect = GetComponent<RectTransform>();
    }

    void LateUpdate()
    {
        if (!canvasRect) return;
        var box = GetComponent<BoxCollider2D>();
        box.isTrigger = true;

        // Convert RectTransform rect (local space) to world size
        Vector2 size = canvasRect.rect.size;
        Vector3 scale = canvasRect.lossyScale;
        box.size = new Vector2(size.x * scale.x, size.y * scale.y);
        box.offset = Vector2.zero;

        // Match world position
        transform.position = canvasRect.position;
    }
}