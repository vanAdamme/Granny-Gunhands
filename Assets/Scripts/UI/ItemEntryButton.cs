using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemEntryButton : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private Button useButton;

    private ItemDefinition def;
    private System.Action<ItemDefinition> onUse;

    public void Bind(ItemInventory.Stack stack, System.Action<ItemDefinition> onUse)
    {
        def = stack.def;
        this.onUse = onUse;

        if (icon)
        {
            icon.sprite = stack.def ? stack.def.Icon : null;
            icon.type = Image.Type.Simple;
            icon.preserveAspect = true;
        }
        if (nameText) nameText.text = stack.def ? stack.def.DisplayName : "(null)";
        if (countText) countText.text = stack.def && stack.def.Stackable ? $"×{stack.count}" : "";
        if (useButton)
        {
            useButton.onClick.RemoveAllListeners();
            useButton.onClick.AddListener(() => this.onUse?.Invoke(def));
        }
    }
}