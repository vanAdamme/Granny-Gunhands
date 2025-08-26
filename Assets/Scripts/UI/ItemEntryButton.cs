using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

[RequireComponent(typeof(CanvasGroup))] // <- guarantees it exists
public class ItemEntryButton : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
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
    private Canvas rootCanvas;
    private RectTransform dragGhost;
    private Image dragGhostImage;
    private CanvasGroup selfCanvasGroup;

    public ItemDefinition Definition => def;
    public ItemInventory SourceInventory => sourceInventory;

    void Awake()
    {
        // Cache the CanvasGroup the moment we exist
        selfCanvasGroup = GetComponent<CanvasGroup>();
        // Find the nearest canvas (even if disabled)
        rootCanvas = GetComponentInParent<Canvas>(includeInactive: true);
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
        }
        if (nameText)  nameText.text  = def ? def.DisplayName : "(null)";
        if (countText) countText.text = def && def.Stackable ? $"×{stack.count}" : "";

        if (useButton)
        {
            useButton.onClick.RemoveAllListeners();
            useButton.onClick.AddListener(() => this.onUse?.Invoke(def));
            useButton.interactable = canUse;
        }

        // Safety nets if this row was instantiated under a different canvas at runtime
        if (!selfCanvasGroup) selfCanvasGroup = GetComponent<CanvasGroup>();
        if (!rootCanvas)      rootCanvas      = GetComponentInParent<Canvas>(includeInactive: true);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!def || sourceInventory == null) return;

        // Double-safety: if somehow missing, add one now
        if (!selfCanvasGroup) selfCanvasGroup = gameObject.AddComponent<CanvasGroup>();
        selfCanvasGroup.blocksRaycasts = false; // allow drops to receive raycasts

        if (!rootCanvas) rootCanvas = GetComponentInParent<Canvas>(includeInactive: true);
        if (!rootCanvas) return; // no canvas? nothing to draw the ghost into

        // Create a ghost sprite that follows the cursor
        dragGhost = new GameObject("DragGhost", typeof(RectTransform), typeof(CanvasGroup), typeof(Image)).GetComponent<RectTransform>();
        dragGhost.SetParent(rootCanvas.transform, worldPositionStays: false);
        dragGhost.pivot = new Vector2(0.5f, 0.5f);

        var size = (icon && icon.sprite) ? icon.sprite.rect.size : new Vector2(64, 64);
        dragGhost.sizeDelta = size;

        dragGhostImage = dragGhost.GetComponent<Image>();
        dragGhostImage.raycastTarget = false;
        dragGhostImage.preserveAspect = true;
        dragGhostImage.sprite = icon ? icon.sprite : null;
        dragGhostImage.color = new Color(1f, 1f, 1f, 0.9f);

        dragGhost.position = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragGhost) dragGhost.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (selfCanvasGroup) selfCanvasGroup.blocksRaycasts = true;
        if (dragGhost) Destroy(dragGhost.gameObject);
    }
}