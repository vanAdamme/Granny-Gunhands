using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemSlotUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI countText;
    [SerializeField] private Button useButton;

    private int index;
    private Action<int> onUse;

    public void Setup(Sprite sprite, int count, int index, Action<int> onUse)
    {
        this.index = index;
        this.onUse = onUse;

        if (icon) icon.sprite = sprite;
        if (countText) countText.text = count > 1 ? count.ToString() : string.Empty;

        if (useButton)
        {
            useButton.onClick.RemoveAllListeners();
            useButton.onClick.AddListener(() => this.onUse?.Invoke(this.index));
        }
    }
}