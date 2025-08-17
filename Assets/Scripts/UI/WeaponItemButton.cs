using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WeaponItemButton : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] private Button button;
    [SerializeField] private Image iconImage;
    [SerializeField] private Image background;     // tint by rarity
    [SerializeField] private TMP_Text label;       // optional
    [SerializeField] private GameObject leftBadge; // small "L" indicator
    [SerializeField] private GameObject rightBadge;// small "R" indicator

    private int myIndex;
    private System.Action<int> onClick;

    public void Bind(Weapon w, int index, RaritySettings rar, System.Action<int> onClickHandler)
    {
        myIndex = index;
        onClick = onClickHandler;

        if (iconImage) iconImage.sprite = w ? w.icon : null;
        if (label)     label.text = w && w.Definition ? w.Definition.DisplayName : (w ? w.name : "—");

        if (background && rar && w && w.Definition)
            background.color = rar.Get(w.Definition.Rarity).colour;

        if (button)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke(myIndex));
        }
    }

    public void RefreshEquipped(Weapon left, Weapon right)
    {
        bool isLeft  = left  && left  == GetWeapon();
        bool isRight = right && right == GetWeapon();

        if (leftBadge)  leftBadge.SetActive(isLeft);
        if (rightBadge) rightBadge.SetActive(isRight);
    }

    private Weapon GetWeapon()
    {
        // If you want to be ultra-robust, you can store a direct Weapon ref on Bind.
        // For now, we fetch from the icon's component hierarchy (iconImage lives under a button that won't be re-used across different weapons during the panel's life).
        return null; // not used; we’ll wire properly from InventoryUI when refreshing.
    }

    // Helper so InventoryUI can drive badges without hacks
    public void SetEquipped(bool isLeft, bool isRight)
    {
        if (leftBadge)  leftBadge.SetActive(isLeft);
        if (rightBadge) rightBadge.SetActive(isRight);
    }
}