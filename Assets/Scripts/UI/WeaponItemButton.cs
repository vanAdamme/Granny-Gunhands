using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class WeaponItemButton : MonoBehaviour,
    IDropHandler, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    [Header("Wiring")]
    [SerializeField] private Button button;
    [SerializeField] private Image iconImage;
    [SerializeField] private Image background;        // tint via RaritySettings
    [SerializeField] private TMP_Text label;          // weapon name (optional)
    [SerializeField] private GameObject leftBadge;    // small "L"
    [SerializeField] private GameObject rightBadge;   // small "R"

    [Header("Drag & Drop Feedback")]
    [SerializeField] private Image dropHighlight;     // overlay tint image on this row
    [SerializeField] private Color validDropTint   = new Color(0f, 1f, 0f, 0.25f);
    [SerializeField] private Color invalidDropTint = new Color(1f, 0f, 0f, 0.25f);
    [SerializeField, Min(0f)] private float flashDuration = 0.18f;

    [Header("Rules")]
    [SerializeField] private bool strictCategoryMatch = true; // enforce upgrade.category == weapon.category

    // Runtime
    private int myIndex;
    private System.Action<int> onClick;
    private Weapon myWeapon;
    private RaritySettings rarityRef;

    // Listen for runtime icon changes (fired by the weapon)
    private GenericProjectileWeapon subscribedIconSource;

    public void Bind(Weapon weapon, int index, RaritySettings rar, System.Action<int> onClickHandler)
    {
        // Unsubscribe old source
        if (subscribedIconSource)
            subscribedIconSource.IconChanged -= OnWeaponIconChanged;

        myIndex    = index;
        myWeapon   = weapon;
        rarityRef  = rar;
        onClick    = onClickHandler;

        if (iconImage)
        {
            iconImage.preserveAspect = true;

            // Prefer weapon's runtime icon; fall back to definition icon
            Sprite s = null;
            if (weapon)
            {
                if (weapon.icon)                      s = weapon.icon;
                else if (weapon.Definition?.Icon)     s = weapon.Definition.Icon;
            }

            iconImage.sprite  = s;
            iconImage.enabled = s != null;
            iconImage.color   = Color.white;
        }

        if (label)
            label.text = weapon && weapon.Definition
                        ? weapon.Definition.DisplayName
                        : (weapon ? weapon.name : "—");

        if (background && rar && weapon && weapon.Definition)
            background.color = rar.Get(weapon.Definition.Rarity).colour;

        if (button)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke(myIndex));
        }

        if (dropHighlight) dropHighlight.enabled = false;

        // Subscribe to runtime icon updates if available
        subscribedIconSource = weapon as GenericProjectileWeapon;
        if (subscribedIconSource)
            subscribedIconSource.IconChanged += OnWeaponIconChanged;
    }

    private void OnDestroy()
    {
        if (subscribedIconSource)
            subscribedIconSource.IconChanged -= OnWeaponIconChanged;
    }

    private void OnWeaponIconChanged(Sprite s)
    {
        if (!iconImage) return;
        iconImage.sprite  = s;
        iconImage.enabled = s != null;
    }

    public void SetEquipped(bool isLeft, bool isRight)
    {
        if (leftBadge)  leftBadge.SetActive(isLeft);
        if (rightBadge) rightBadge.SetActive(isRight);
    }

    // ---------- Drag & Drop ----------
    public void OnPointerEnter(PointerEventData eventData)
    {
        var entry = GetDraggedEntry(eventData);
        if (!entry || !dropHighlight) return;

        var upgrade = entry.Definition as WeaponUpgradeItemDefinition;
        if (!upgrade) return;

        bool valid = IsValidUpgradeForThisWeapon(upgrade, out var reason);
        dropHighlight.color   = valid ? validDropTint : invalidDropTint;
        dropHighlight.enabled = true;

        // Tooltip preview
        if (UpgradeTooltipController.Instance)
        {
            if (valid)
            {
                if (upgrade.TryPreviewFor(myWeapon, out var delta, out var note) && !delta.IsEmpty)
                {
                    var body = delta.ToMultiline();
                    if (!string.IsNullOrEmpty(note)) body += $"\n{note}";
                    UpgradeTooltipController.Instance.Show(eventData.position,
                        $"Apply to {myWeapon.Definition.DisplayName}", body);
                }
                else
                {
                    UpgradeTooltipController.Instance.Show(eventData.position,
                        $"Apply to {myWeapon.Definition.DisplayName}", "Will upgrade.");
                }
            }
            else
            {
                UpgradeTooltipController.Instance.Show(eventData.position, "Can't apply", reason);
            }
        }
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        UpgradeTooltipController.Instance?.Follow(eventData.position);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (dropHighlight) dropHighlight.enabled = false;
        UpgradeTooltipController.Instance?.Hide();
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (dropHighlight) dropHighlight.enabled = false;
        UpgradeTooltipController.Instance?.Hide();

        var entry   = GetDraggedEntry(eventData);
        var upgrade = entry ? entry.Definition as WeaponUpgradeItemDefinition : null;

        if (!entry || !upgrade || entry.SourceInventory == null || myWeapon == null)
            return;

        if (!IsValidUpgradeForThisWeapon(upgrade, out var reason))
        {
            UIController.Instance?.ShowToast(reason);
            Flash(invalidDropTint);
            return;
        }

        // Apply via the ScriptableObject (returns true only if something changed)
        if (upgrade.TryApplyTo(myWeapon, out var applied, out var applyReason) && applied > 0)
        {
            // Consume one upgrade item
            entry.SourceInventory.Remove(upgrade, 1);

            // UI feedback
            UIController.Instance?.ShowToast($"Upgraded {myWeapon.Definition.DisplayName}", myWeapon.icon);
            OnWeaponIconChanged(myWeapon.icon); // ensure immediate refresh
            Flash(validDropTint);
        }
        else
        {
            UIController.Instance?.ShowToast(string.IsNullOrEmpty(applyReason) ? "No upgrade applied." : applyReason);
            Flash(invalidDropTint);
        }
    }

    // ---------- Helpers ----------
    private static ItemEntryButton GetDraggedEntry(PointerEventData e)
    {
        if (e == null || !e.pointerDrag) return null;
        // pointerDrag may be a child; climb to parent row
        return e.pointerDrag.GetComponentInParent<ItemEntryButton>();
    }

    private bool IsValidUpgradeForThisWeapon(WeaponUpgradeItemDefinition upgrade, out string reason)
    {
        reason = "";
        if (!strictCategoryMatch) return true;

        if (!myWeapon || !myWeapon.Definition)
        {
            reason = "No weapon.";
            return false;
        }

        // Definition exposes both 'Category' (property) and 'category' (field) per our definition file
        var weaponCat = myWeapon.Definition.Category;
        if (weaponCat != upgrade.category)
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
}