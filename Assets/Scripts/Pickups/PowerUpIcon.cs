using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PowerUpIcon : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private TextMeshProUGUI timer;

    public void SetData(Sprite icon, string nameText)
    {
        iconImage.sprite = icon;
        iconImage.enabled = icon != null;
        label.text = nameText;
    }

    public void SetTime(float remaining)
    {
        if (remaining < 0f)
        {
            timer.enabled = false; // permanent
        }
        else
        {
            timer.enabled = true;
            timer.text = $"{remaining:0.0}s";
        }
    }
}
