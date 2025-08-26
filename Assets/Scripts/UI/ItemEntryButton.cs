using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

// Guarantees we can toggle blocksRaycasts safely
[RequireComponent(typeof(CanvasGroup))]
public class ItemEntryButton : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IInitializePotentialDragHandler
{
    [Header("UI")]
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private Button useButton;

    // Data for this row
    private ItemDefinition def;
    private ItemInventory sourceInventory;
    private System.Action<ItemDefinition> onUse;

    // Drag visuals
    private Canvas rootCanvas;              // top-most canvas
    private RectTransform dragGhost;        // ghost rect
    private Image dragGhostImage;           // ghost image
    private CanvasGroup selfCanvasGroup;    // to disable raycast during drag

    public ItemDefinition Definition => def;
    public ItemInventory SourceInventory => sourceInventory;

    void Awake()
    {
        selfCanvasGroup = GetComponent<CanvasGroup>();
        var localCanvas = GetComponentInParent<Canvas>(includeInactive: true);
        rootCanvas = localCanvas ? localCanvas.rootCanvas : null;
    }

    // New signature passes source inventory + canUse
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
            icon.color = Color.white; // ensure alpha is 1
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
            var localCanvas = GetComponentInParent<Canvas>(includeInactive: true);
            rootCanvas = localCanvas ? localCanvas.rootCanvas : null;
        }
    }

    // This helps beat ScrollRect’s drag threshold stealing
    public void OnInitializePotentialDrag(PointerEventData eventData)
    {
        eventData.useDragThreshold = false;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!def || sourceInventory == null) return;

        if (!selfCanvasGroup) selfCanvasGroup = gameObject.AddComponent<CanvasGroup>();
        selfCanvasGroup.blocksRaycasts = false; // allow drops to receive raycasts

        if (!rootCanvas)
        {
            var localCanvas = GetComponentInParent<Canvas>(includeInactive: true);
            rootCanvas = localCanvas ? localCanvas.rootCanvas : null;
        }
        if (!rootCanvas) return; // no canvas, no ghost

        // Create a ghost under the ROOT canvas so it isn't clipped by masks/viewport
        dragGhost = new GameObject("DragGhost", typeof(RectTransform), typeof(CanvasGroup), typeof(Image))
            .GetComponent<RectTransform>();
        dragGhost.SetParent(rootCanvas.transform, worldPositionStays: false);
        dragGhost.anchorMin = dragGhost.anchorMax = new Vector2(0.5f, 0.5f); // centered anchors
        dragGhost.pivot = new Vector2(0.5f, 0.5f);
        dragGhost.SetAsLastSibling();

        // Size: mirror the visible icon if present; fallback to 64x64
        Vector2 size = new Vector2(64, 64);
        if (icon && icon.sprite)
        {
            var iconRT = icon.rectTransform;
            size = iconRT ? iconRT.rect.size : size;
        }
        dragGhost.sizeDelta = size;

        dragGhostImage = dragGhost.GetComponent<Image>();
        dragGhostImage.raycastTarget = false;
        dragGhostImage.preserveAspect = true;
        dragGhostImage.sprite = icon ? icon.sprite : null;
        dragGhostImage.color = new Color(1f, 1f, 1f, 0.9f);

        // Initial placement (screen -> local anchoredPosition)
        dragGhost.anchoredPosition = ScreenToCanvasLocal(eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!dragGhost) return;
        dragGhost.anchoredPosition = ScreenToCanvasLocal(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (selfCanvasGroup) selfCanvasGroup.blocksRaycasts = true;
        if (dragGhost) Destroy(dragGhost.gameObject);
        UpgradeTooltipController.Instance?.Hide();
    }

    // Convert screen-space mouse/touch to canvas local space
    private Vector2 ScreenToCanvasLocal(Vector2 screenPos)
    {
        if (!rootCanvas) return screenPos;

        RectTransform canvasRT = rootCanvas.transform as RectTransform;
        Camera cam = null;
        if (rootCanvas.renderMode == RenderMode.ScreenSpaceCamera ||
            rootCanvas.renderMode == RenderMode.WorldSpace)
        {
            cam = rootCanvas.worldCamera;
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRT, screenPos, cam, out var localPoint);
        return localPoint;
    }
}