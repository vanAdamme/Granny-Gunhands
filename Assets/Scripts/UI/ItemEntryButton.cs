using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

[RequireComponent(typeof(CanvasGroup))]
public class ItemEntryButton : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IInitializePotentialDragHandler
{
    [Header("UI")]
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private Button useButton;

    // Data
    private ItemDefinition def;
    private ItemInventory sourceInventory;
    private System.Action<ItemDefinition> onUse;

    // Drag
    private Canvas rootCanvas;
    private RectTransform dragGhost;
    private Image dragGhostImage;
    private CanvasGroup selfCanvasGroup;
    public static bool IsDragging { get; private set; }

    // Internal coroutine runner (one per app)
    private static Runner runner;
    private class Runner : MonoBehaviour { }

    // Optional: ensure only one ghost exists globally
    private static RectTransform activeGhost;

    public ItemDefinition Definition => def;
    public ItemInventory SourceInventory => sourceInventory;

    void Awake()
    {
        selfCanvasGroup = GetComponent<CanvasGroup>();
        var localCanvas = GetComponentInParent<Canvas>(includeInactive: true);
        rootCanvas = localCanvas ? localCanvas.rootCanvas : null;
    }

    public void Bind(ItemInventory.Stack stack, ItemInventory srcInventory, bool canUse, System.Action<ItemDefinition> onUse)
    {
        def = stack.def;
        sourceInventory = srcInventory;
        this.onUse = onUse;

        if (icon)
        {
            icon.sprite = def ? def.Icon : null;
            icon.type = Image.Type.Simple;
            icon.preserveAspect = true;
            icon.enabled = icon.sprite != null;
            icon.color = Color.white;
        }
        if (nameText)  nameText.text  = def ? def.DisplayName : "(null)";
        if (countText) countText.text = def && def.Stackable ? $"×{stack.count}" : "";

        if (useButton)
        {
            useButton.onClick.RemoveAllListeners();
            useButton.onClick.AddListener(() => this.onUse?.Invoke(def));
            useButton.interactable = canUse;
        }

        if (!selfCanvasGroup) selfCanvasGroup = GetComponent<CanvasGroup>();
        if (!rootCanvas)
        {
            var lc = GetComponentInParent<Canvas>(includeInactive: true);
            rootCanvas = lc ? lc.rootCanvas : null;
        }
    }

    public void OnInitializePotentialDrag(PointerEventData eventData)
    {
        eventData.useDragThreshold = false;
    }

   public void OnBeginDrag(PointerEventData eventData)
    {
        if (!def || sourceInventory == null) return;

        IsDragging = true;

        if (!selfCanvasGroup) selfCanvasGroup = gameObject.AddComponent<CanvasGroup>();
        selfCanvasGroup.blocksRaycasts = false;

        if (!rootCanvas)
        {
            var lc = GetComponentInParent<Canvas>(includeInactive: true);
            rootCanvas = lc ? lc.rootCanvas : null;
        }
        if (!rootCanvas) return;

        // Kill any previous ghost
        if (activeGhost) Destroy(activeGhost.gameObject);

        var go = new GameObject("DragGhost", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        dragGhost = go.GetComponent<RectTransform>();
        activeGhost = dragGhost;

        dragGhost.SetParent(rootCanvas.transform, false);
        dragGhost.anchorMin = dragGhost.anchorMax = new Vector2(0.5f, 0.5f);
        dragGhost.pivot = new Vector2(0.5f, 0.5f);
        dragGhost.localScale = Vector3.one;
        dragGhost.SetAsLastSibling();

        var size = (icon && icon.sprite) ? icon.rectTransform.rect.size : new Vector2(64, 64);
        dragGhost.sizeDelta = size;

        dragGhostImage = dragGhost.GetComponent<Image>();
        dragGhostImage.raycastTarget = false;
        dragGhostImage.preserveAspect = true;
        dragGhostImage.sprite = icon ? icon.sprite : null;
        dragGhostImage.color = new Color(1f, 1f, 1f, 0.9f);

        dragGhost.anchoredPosition = ScreenToCanvasLocal(eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragGhost) dragGhost.anchoredPosition = ScreenToCanvasLocal(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData) => CleanupDragArtifacts();

    // Crucial: if the row is destroyed/disabled during a drop, OnEndDrag won't run.
    // Clean up the ghost here too.
    void OnDisable() => CleanupDragArtifacts();
    void OnDestroy() => CleanupDragArtifacts();

    private void CleanupDragArtifacts()
    {
        if (selfCanvasGroup) selfCanvasGroup.blocksRaycasts = true;

        if (dragGhost)
        {
            Destroy(dragGhost.gameObject);
            if (activeGhost == dragGhost) activeGhost = null;
            dragGhost = null;
            dragGhostImage = null;
        }

        UpgradeTooltipController.Instance?.Hide();
        IsDragging = false;
    }

    private Vector2 ScreenToCanvasLocal(Vector2 screenPos)
    {
        if (!rootCanvas) return screenPos;
        var canvasRT = (RectTransform)rootCanvas.transform;
        Camera cam = null;
        if (rootCanvas.renderMode == RenderMode.ScreenSpaceCamera ||
            rootCanvas.renderMode == RenderMode.WorldSpace)
            cam = rootCanvas.worldCamera;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, screenPos, cam, out var local);
        return local;
    }

    private static void EnsureRunner()
    {
        if (runner) return;
        var go = new GameObject("ItemEntryButton_Coroutines");
        DontDestroyOnLoad(go);
        runner = go.AddComponent<Runner>();
    }

    // ---- public static helper for invalid-drop "snap" (here: shrink+fade) ----
    public static void ShrinkAndDestroyActiveGhost(float duration = 0.12f)
    {
        if (!activeGhost) return;
        EnsureRunner();
        runner.StartCoroutine(ShrinkRoutine(duration));
    }

    private static System.Collections.IEnumerator ShrinkRoutine(float duration)
    {
        var ghost = activeGhost;
        if (!ghost) yield break;

        var cg = ghost.GetComponent<CanvasGroup>() ?? ghost.gameObject.AddComponent<CanvasGroup>();
        Vector3 startScale = ghost.localScale;
        float startAlpha = cg.alpha;
        float t = 0f;

        while (t < duration && ghost)
        {
            t += Time.unscaledDeltaTime;
            float k = 1f - Mathf.Clamp01(t / duration);
            ghost.localScale = startScale * (0.85f + 0.15f * k); // slight shrink
            cg.alpha = startAlpha * k;
            yield return null;
        }

        if (ghost) Object.Destroy(ghost.gameObject);
        if (activeGhost == ghost) activeGhost = null;
    }
}