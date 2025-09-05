using UnityEngine;
using TMPro;

public class DamageSinceSpecialCounter : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private string format = "DMG: {0}";
    [SerializeField, Min(0)] private int decimals = 0;

    private float total;

    void OnEnable()
    {
        PlayerDamageEvents.DamagedEnemy += OnDamage;
        SpecialEvents.Fired += ResetCounter;
        Refresh();
    }

    void OnDisable()
    {
        PlayerDamageEvents.DamagedEnemy -= OnDamage;
        SpecialEvents.Fired -= ResetCounter;
    }

    void Awake()
    {
        if (!label) label = GetComponent<TMP_Text>(); // auto-resolve if you drop it on the same GO
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
        if (!label) return;
        label.text = string.Format(format, System.Math.Round(total, decimals));
    }
}