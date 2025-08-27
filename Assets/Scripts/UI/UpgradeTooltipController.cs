	using TMPro;
using UnityEngine;

public class UpgradeTooltipController : MonoBehaviour
{
    public static UpgradeTooltipController Instance { get; private set; }

    [SerializeField] private RectTransform panel;   // root panel
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;

    private Canvas rootCanvas;

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        var c = GetComponentInParent<Canvas>(true);
        rootCanvas = c ? c.rootCanvas : null;

        Hide();
    }

    public void Show(Vector2 screenPos, string title, string body)
    {
        if (!panel) return;
        titleText?.SetText(title);
        bodyText?.SetText(body);

        var canvasRT = rootCanvas ? (RectTransform)rootCanvas.transform : panel;
        Camera cam = null;
        if (rootCanvas && (rootCanvas.renderMode == RenderMode.ScreenSpaceCamera || rootCanvas.renderMode == RenderMode.WorldSpace))
            cam = rootCanvas.worldCamera;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, screenPos, cam, out var local);
        panel.SetParent(canvasRT, false);
        panel.anchoredPosition = local + new Vector2(16f, -16f); // slight offset from cursor
        panel.gameObject.SetActive(true);
    }

    public void Follow(Vector2 screenPos)
    {
        if (!panel || !panel.gameObject.activeSelf) return;
        var canvasRT = rootCanvas ? (RectTransform)rootCanvas.transform : panel;
        Camera cam = null;
        if (rootCanvas && (rootCanvas.renderMode == RenderMode.ScreenSpaceCamera || rootCanvas.renderMode == RenderMode.WorldSpace))
            cam = rootCanvas.worldCamera;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, screenPos, cam, out var local);
        panel.anchoredPosition = local + new Vector2(16f, -16f);
    }

    public void Hide()
    {
        if (panel) panel.gameObject.SetActive(false);
    }
}