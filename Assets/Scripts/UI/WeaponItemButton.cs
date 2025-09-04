using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class WeaponItemButton : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Wiring")]
    [SerializeField] private Button button;
    [SerializeField] private Image iconImage;
    [SerializeField] private Image background;        // tint by rarity
    [SerializeField] private TMP_Text label;          // optional
    [SerializeField] private GameObject leftBadge;    // small "L"
    [SerializeField] private GameObject rightBadge;   // small "R"

    [Header("Drag & Drop Feedback")]
    [SerializeField] private Image dropHighlight;     // thin overlay image on this row
    [SerializeField] private Color validDropTint   = new Color(0f, 1f, 0f, 0.25f);
    [SerializeField] private Color invalidDropTint = new Color(1f, 0f, 0f, 0.25f);
    [SerializeField, Min(0f)] private float flashDuration = 0.18f;

    [Header("Rules")]
    [SerializeField] private bool strictCategoryMatch = true; // enforce upgrade.category == weapon.category

    [Header("Services (optional)")]
    [Tooltip("Optional. If set, toasts will be sent here first; otherwise we fallback to UIController.Instance.")]
    [SerializeField] private MonoBehaviour toastServiceSource; // should implement IToastService
    private IToastService toast;

    private int myIndex;
    private System.Action<int> onClick;
    private Weapon myWeapon;
    private RaritySettings rarityRef;

    void Awake()
    {
        toast = toastServiceSource as IToastService; // may be null; we fallback at call site
        if (dropHighlight) dropHighlight.enabled = false;
    }

    public void Bind(Weapon w, int index, RaritySettings rar, System.Action<int> onClickHandler)
    {
        myIndex = index;
        onClick = onClickHandler;
        myWeapon = w;
        rarityRef = rar;

        if (iconImage)
        {
            iconImage.preserveAspect = true;

            // Prefer the icon on the definition, fall back to weapon.icon
            Sprite s = null;
            if (w)
            {
                if (w.Definition && w.Definition.Icon) s = w.Definition.Icon;
                else if (w.icon)                       s = w.icon;
            }

            iconImage.sprite   = s;
            iconImage.enabled  = s != null;
            iconImage.color    = Color.white; // ensure alpha is 1
        }

        if (label)
            label.text = w && w.Definition ? w.Definition.DisplayName : (w ? w.name : "—");

        if (background && rar && w && w.Definition)
            background.color = rar.Get(w.Definition.Rarity).colour;

        if (button)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke(myIndex));
        }

        if (dropHighlight) dropHighlight.enabled = false;
    }

    public void SetEquipped(bool isLeft, bool isRight)
    {
        if (leftBadge)  leftBadge.SetActive(isLeft);
        if (rightBadge) rightBadge.SetActive(isRight);
    }

    // ---------- Drag & Drop ----------
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!dropHighlight) return;

        var entry = GetDraggedEntry(eventData);
        if (!entry) return;

        var upgrade = entry.Definition as WeaponUpgradeItemDefinition;
        if (!upgrade) return;

        bool valid = IsValidUpgradeForThisWeapon(upgrade, out _);
        dropHighlight.color = valid ? validDropTint : invalidDropTint;
        dropHighlight.enabled = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (dropHighlight) dropHighlight.enabled = false;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (dropHighlight) dropHighlight.enabled = false;

        var entry   = GetDraggedEntry(eventData);
        var upgrade = entry ? entry.Definition as WeaponUpgradeItemDefinition : null;

        if (!entry || !upgrade || entry.SourceInventory == null || myWeapon == null)
            return;

        if (!IsValidUpgradeForThisWeapon(upgrade, out var reason))
        {
            ShowToast(reason);
            Flash(invalidDropTint);
            return;
        }

        // Apply upgrade via definition-driven path (weapon implements IUpgradableWeapon).
        if (upgrade.TryApplyTo(myWeapon, out var levelsApplied, out var applyReason))
        {
            entry.SourceInventory.Remove(upgrade, 1);
            var name = myWeapon.Definition ? myWeapon.Definition.DisplayName : myWeapon.name;
            var icon = (myWeapon.Definition && myWeapon.Definition.Icon) ? myWeapon.Definition.Icon : myWeapon.icon;
            ShowToast($"Upgraded {name} (+{levelsApplied})", icon);
            Flash(validDropTint);
            return;
        }

        // Failed to apply (e.g., already max level)
        ShowToast(string.IsNullOrEmpty(applyReason) ? "Upgrade could not be applied." : applyReason);
        Flash(invalidDropTint);
    }

    // ---------- Helpers ----------
    private static ItemEntryButton GetDraggedEntry(PointerEventData e)
    {
        if (e == null || !e.pointerDrag) return null;
        return e.pointerDrag.GetComponentInParent<ItemEntryButton>();
    }

    private bool IsValidUpgradeForThisWeapon(WeaponUpgradeItemDefinition upgrade, out string reason)
    {
        reason = string.Empty;

        if (!strictCategoryMatch) return true;

        if (myWeapon == null || myWeapon.Definition == null)
        {
            reason = "Weapon not set.";
            return false;
        }

        // Use definition data directly (no reflection)
        if (myWeapon.Definition.category != upgrade.category)
        {
            reason = $"Requires {upgrade.category} weapon.";
            return false;
        }

        return true;
    }

    private void Flash(Color tint)
    {
        if (!dropHighlight) return;
        StopAllCoroutines();
        StartCoroutine(FlashRoutine(tint));
    }

    private System.Collections.IEnumerator FlashRoutine(Color tint)
    {
        dropHighlight.enabled = true;
        var prev = dropHighlight.color;
        dropHighlight.color = tint;
        yield return new WaitForSecondsRealtime(Mathf.Max(0.06f, flashDuration));
        dropHighlight.color = prev;
        dropHighlight.enabled = false;
    }

    private void ShowToast(string message, Sprite icon = null, float duration = 2.2f)
    {
        // Prefer injected toast service; fallback to UIController singleton if present
        if (toast != null) { toast.Show(message, icon, duration); return; }
        UIController.Instance?.ShowToast(message, icon, duration);
    }
}