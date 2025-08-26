using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class WeaponItemButton : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
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

    private int myIndex;
    private System.Action<int> onClick;
    private Weapon myWeapon;
    private RaritySettings rarityRef;

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

        bool valid = IsValidUpgradeForThisWeapon(upgrade, out var reason);
        dropHighlight.color = valid ? validDropTint : invalidDropTint;
        dropHighlight.enabled = true;

        // Show preview tooltip
        if (valid && UpgradeTooltipController.Instance)
        {
            if (upgrade.TryPreviewFor(myWeapon, out var delta, out var note) && !delta.IsEmpty)
            {
                var body = delta.ToMultiline();
                if (!string.IsNullOrEmpty(note)) body += $"\n{note}";
                UpgradeTooltipController.Instance.Show(eventData.position, $"Apply to {myWeapon.Definition.DisplayName}", body);
            }
            else
            {
                UpgradeTooltipController.Instance.Show(eventData.position, $"Apply to {myWeapon.Definition.DisplayName}", "Will upgrade.");
            }
        }
        else if (!valid && UpgradeTooltipController.Instance)
        {
            UpgradeTooltipController.Instance.Show(eventData.position, "Can't apply", reason);
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

        var entry = GetDraggedEntry(eventData);
        var upgrade = entry ? entry.Definition as WeaponUpgradeItemDefinition : null;
        if (!entry || !upgrade || entry.SourceInventory == null || myWeapon == null)
            return;

        if (!IsValidUpgradeForThisWeapon(upgrade, out var reason))
        {
            UIController.Instance?.ShowToast(reason);
            Flash(invalidDropTint);
            return;
        }

        if (upgrade.TryApplyTo(myWeapon, out var applied, out var applyReason) && applied > 0)
        {
            entry.SourceInventory.Remove(upgrade, 1);
            UIController.Instance?.ShowToast($"Upgraded {myWeapon.Definition.DisplayName}", myWeapon.icon);
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
        // pointerDrag may be a child under the row; climb to parent
        return e.pointerDrag.GetComponentInParent<ItemEntryButton>();
    }

    private bool IsValidUpgradeForThisWeapon(WeaponUpgradeItemDefinition upgrade, out string reason)
    {
        reason = string.Empty;

        if (!strictCategoryMatch) return true;

        if (!TryGetWeaponCategory(myWeapon, out var weaponCat))
        {
            reason = "Weapon has no category set.";
            return false;
        }

        if (weaponCat != upgrade.category)
        {
            reason = $"Requires {upgrade.category} weapon.";
            return false;
        }

        return true;
    }

    private static bool TryGetWeaponCategory(Weapon weapon, out WeaponCategory cat)
    {
        cat = default;
        if (!weapon || !weapon.Definition) return false;

        var def = weapon.Definition;
        var t = def.GetType();

        // Look for a public property "Category"
        var p = t.GetProperty("Category", BindingFlags.Instance | BindingFlags.Public);
        if (p != null && p.PropertyType == typeof(WeaponCategory))
        {
            cat = (WeaponCategory)p.GetValue(def, null);
            return true;
        }

        // Or a public field "category"
        var f = t.GetField("category", BindingFlags.Instance | BindingFlags.Public);
        if (f != null && f.FieldType == typeof(WeaponCategory))
        {
            cat = (WeaponCategory)f.GetValue(def);
            return true;
        }

        return false;
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