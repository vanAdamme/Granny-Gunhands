using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private WeaponInventory inventory;
    [SerializeField] private RaritySettings raritySettings;

    [Header("Layout")]
    [SerializeField] private Transform gridRoot;
    [SerializeField] private WeaponItemButton itemButtonPrefab;
    [SerializeField] private Button assignLeftButton;
    [SerializeField] private Button assignRightButton;

    [Header("Panel")]
    [SerializeField] private CanvasGroup panel;

    [Header("Behaviour")]
    [SerializeField] private bool pauseWhileOpen = true;
    [SerializeField] private bool pauseAudio = true;
    [SerializeField] private bool manageCursor = true;

    [Header("Input")]
    [SerializeField] private MonoBehaviour inputServiceSource; // drag your InputService here
    private IInputService input;

    float savedTimeScale = 1f;
    CursorLockMode savedLockState;
    bool savedCursorVisible;

    private readonly List<WeaponItemButton> items = new();
    private Hand assignTarget = Hand.Left;
    private void OnToggle() => Toggle();

    void Awake()
    {
        if (!inventory) inventory = FindFirstObjectByType<WeaponInventory>();
        input = inputServiceSource as IInputService;
        if (input == null) input = FindFirstObjectByType<InputService>();

        assignLeftButton?.onClick.AddListener(() => { assignTarget = Hand.Left;  RefreshAssignButtons(); });
        assignRightButton?.onClick.AddListener(() => { assignTarget = Hand.Right; RefreshAssignButtons(); });

        if (inventory != null)
        {
            inventory.OnEquippedChanged += (_, __) => RefreshEquippedBadges();
            inventory.InventoryChanged   += Rebuild;
        }
    }

    void Start()
    {
        // One-time initial state; panel GO stays enabled
        Show(false);
    }

    void OnEnable()
    {
        Rebuild();
        RefreshAssignButtons();
        if (input != null) input.ToggleInventory += OnToggle; // No Show(false) here
    }

    void OnDisable()
    {
        if (input != null) input.ToggleInventory -= OnToggle;
    }

    private void OnToggleInventory() => Toggle();
    public  void Toggle()             => Show(panel.alpha <= 0.01f);

    public void Show(bool show)
    {
        if (!panel) return;

        panel.alpha = show ? 1f : 0f;
        panel.blocksRaycasts = show;
        panel.interactable = show;

        if (show)
        {
            Rebuild();
            RefreshAssignButtons();

            // ask any child inventory panels (like the items list) to refresh too
            var panels = GetComponentsInChildren<IInventoryPanel>(true);
            for (int i = 0; i < panels.Length; i++)
            panels[i].RefreshPanel();

            if (pauseWhileOpen)
            {
                savedTimeScale = Time.timeScale;
                Time.timeScale = 0f;
                if (pauseAudio) AudioListener.pause = true;
            }
            if (manageCursor)
            {
                savedLockState = Cursor.lockState;
                savedCursorVisible = Cursor.visible;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            Pause.Set(true, this);
            input?.EnableUIMap();      // << take over controls for UI
        }
        else
        {
            if (pauseWhileOpen)
            {
                Time.timeScale = savedTimeScale <= 0f ? 1f : savedTimeScale;
                if (pauseAudio) AudioListener.pause = false;
            }
            if (manageCursor)
            {
                Cursor.lockState = savedLockState;
                Cursor.visible = savedCursorVisible;
            }

            Pause.Set(false, this);
            input?.EnablePlayerMap();  // << hand controls back to gameplay
        }
    }

    private void Rebuild()
    {
        for (int i = 0; i < items.Count; i++)
            if (items[i]) Destroy(items[i].gameObject);
        items.Clear();

        if (!inventory || !gridRoot || !itemButtonPrefab) return;

        var list = inventory.GetInventory();
        for (int i = 0; i < list.Count; i++)
        {
            var w = list[i];
            if (!w) continue;

            var btn = Instantiate(itemButtonPrefab, gridRoot);
            btn.Bind(w, i, raritySettings, EquipIndexIntoAssignedHand);
            items.Add(btn);
        }

        RefreshEquippedBadges();
    }

    private void EquipIndexIntoAssignedHand(int index)
    {
        inventory.Equip(assignTarget, index, applyMount: true, raiseEvents: true);
        RefreshEquippedBadges();
    }

    private void RefreshEquippedBadges()
    {
        var left  = inventory ? inventory.Left  : null;
        var right = inventory ? inventory.Right : null;

        var list = inventory?.GetInventory();
        for (int i = 0; i < items.Count; i++)
        {
            var weapon = (list != null && i < list.Count) ? list[i] : null;
            bool isLeft  = weapon && left  && weapon == left;
            bool isRight = weapon && right && weapon == right;
            items[i].SetEquipped(isLeft, isRight);
        }
    }

    private void RefreshAssignButtons()
    {
        if (!assignLeftButton || !assignRightButton) return;
        assignLeftButton.interactable  = assignTarget != Hand.Left;
        assignRightButton.interactable = assignTarget != Hand.Right;
    }
}