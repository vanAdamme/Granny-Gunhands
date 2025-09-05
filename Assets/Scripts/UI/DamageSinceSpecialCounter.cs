using UnityEngine;
using UnityEngine.UI;
#if TMP_PRESENT
using TMPro;
#endif

public class DamageSinceSpecialCounter : MonoBehaviour
{
#if TMP_PRESENT
    [SerializeField] private TMP_Text tmpLabel;
#endif
    [SerializeField] private Text uGuiLabel;
    [SerializeField] private string format = "DMG: {0}";
    [SerializeField, Min(0)] private int decimals = 0;

    private float total;

    void OnEnable()
    {
        PlayerDamageEvents.DamagedEnemy += OnDamage;
        SpecialEvents.Fired += ResetCounter;   // ← listen here
        Refresh();
    }

    void OnDisable()
    {
        PlayerDamageEvents.DamagedEnemy -= OnDamage;
        SpecialEvents.Fired -= ResetCounter;   // ← and unsubscribe
    }

    void OnDamage(float amount)
    {
        total += Mathf.Max(0f, amount);
        Refresh();
    }

    public void ResetCounter()
    {
        total = 0f;
        Refresh();
    }

    void Refresh()
    {
        var text = string.Format(format, System.Math.Round(total, decimals));
#if TMP_PRESENT
        if (tmpLabel) tmpLabel.text = text;
#endif
        if (uGuiLabel) uGuiLabel.text = text;
    }
}