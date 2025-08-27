using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem; // Pointer.current / Mouse.current / Touchscreen.current
#endif

[RequireComponent(typeof(ScrollRect))]
public class ScrollRectEdgeScroller : MonoBehaviour
{
    [SerializeField, Min(0f)] private float edgeThickness = 36f; // px near top/bottom to start scrolling
    [SerializeField, Min(0f)] private float maxSpeed = 1200f;    // px / second
    [SerializeField] private RectTransform viewportOverride;     // optional; else uses scrollRect.viewport

    private ScrollRect scroll;
    private RectTransform viewport;
    private Canvas rootCanvas;

    void Awake()
    {
        scroll = GetComponent<ScrollRect>();
        viewport = viewportOverride ? viewportOverride : scroll.viewport;
        var c = GetComponentInParent<Canvas>(true);
        rootCanvas = c ? c.rootCanvas : null;
    }

    void Update()
    {
        // Only auto-scroll while dragging an inventory row
        if (!ItemEntryButton.IsDragging || !scroll || !viewport || !scroll.content)
            return;

        if (!TryGetPointerScreenPosition(out var screen))
            return;

        // Convert to viewport local
        Camera cam = null;
        if (rootCanvas && (rootCanvas.renderMode == RenderMode.ScreenSpaceCamera || rootCanvas.renderMode == RenderMode.WorldSpace))
            cam = rootCanvas.worldCamera;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, screen, cam, out var local))
            return;

        var rect = viewport.rect;
        // Only scroll if pointer is inside the viewport
        if (local.x < rect.xMin || local.x > rect.xMax || local.y < rect.yMin || local.y > rect.yMax)
            return;

        float topDist    = rect.yMax - local.y; // px from top
        float bottomDist = local.y - rect.yMin; // px from bottom

        float dy = 0f;
        if (topDist <= edgeThickness)
        {
            float t = 1f - Mathf.Clamp01(topDist / edgeThickness);
            dy = -maxSpeed * t; // scroll up
        }
        else if (bottomDist <= edgeThickness)
        {
            float t = 1f - Mathf.Clamp01(bottomDist / edgeThickness);
            dy =  maxSpeed * t; // scroll down
        }

        if (Mathf.Approximately(dy, 0f)) return;

        var content = scroll.content;
        float vpH = rect.height;
        float contentH = content.rect.height;

        if (contentH <= vpH) return; // nothing to scroll

        float maxY = Mathf.Max(0f, contentH - vpH);
        var ap = content.anchoredPosition;
        ap.y = Mathf.Clamp(ap.y + dy * Time.unscaledDeltaTime, 0f, maxY);
        content.anchoredPosition = ap;
    }

    private static bool TryGetPointerScreenPosition(out Vector2 screenPos)
    {
#if ENABLE_INPUT_SYSTEM
        // Prefer the unified Pointer (mouse on desktop, pen, etc.)
        if (Pointer.current != null)
        {
            screenPos = Pointer.current.position.ReadValue();
            return true;
        }

        // Touch fallback
        if (Touchscreen.current != null && Touchscreen.current.touches.Count > 0)
        {
            screenPos = Touchscreen.current.touches[0].position.ReadValue();
            return true;
        }

        screenPos = default;
        return false;
#else
        // Legacy input manager
        screenPos = Input.mousePosition;
        return true;
#endif
    }
}
