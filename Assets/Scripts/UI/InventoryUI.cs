using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private WeaponInventory inventory;
    [SerializeField] private RaritySettings raritySettings;

    [Header("Layout")]
    [SerializeField] private Transform gridRoot;                  // has GridLayoutGroup
    [SerializeField] private WeaponItemButton itemButtonPrefab;   // the tile prefab
    [SerializeField] private Button assignLeftButton;
    [SerializeField] private Button assignRightButton;

    [Header("Panel")]
    [SerializeField] private CanvasGroup panel;                   // for show/hide
    [SerializeField] public KeyCode toggleKey = KeyCode.Tab;

    [Header("Fields")]
    [SerializeField] private bool pauseWhileOpen = true;
    [SerializeField] private bool pauseAudio = true;
    [SerializeField] private bool manageCursor = true;

    float savedTimeScale = 1f;
    CursorLockMode savedLockState;
    bool savedCursorVisible;

    private readonly List<WeaponItemButton> items = new();
    private Hand assignTarget = Hand.Left;

    void Awake()
    {
        if (!inventory) inventory = FindFirstObjectByType<WeaponInventory>();

        assignLeftButton?.onClick.AddListener(() => { assignTarget = Hand.Left;  RefreshAssignButtons(); });
        assignRightButton?.onClick.AddListener(() => { assignTarget = Hand.Right; RefreshAssignButtons(); });

        if (inventory != null)
        {
            inventory.OnEquippedChanged += (_, __) => RefreshEquippedBadges();
            inventory.InventoryChanged   += Rebuild;
        }
    }

    void OnEnable()
    {
        Rebuild();
        RefreshAssignButtons();
        Show(false); // start hidden unless you want it open
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            Toggle();
    }

    public void Toggle() => Show(panel.alpha <= 0.01f);

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
        }
    }

    private void Rebuild()
    {
        // clear
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

        // we know each button maps to an index; compare references
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