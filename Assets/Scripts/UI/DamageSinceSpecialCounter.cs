using UnityEngine;
using TMPro;

public class DamageSinceSpecialCounter : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private string format = "DMG: {0}";
    [SerializeField, Min(0)] private int decimals = 0;

    ISpecialCharge meter;

    void Awake()
    {
        if (!label) label = GetComponent<TMP_Text>();
        meter = FindFirstObjectByType<SpecialChargeSimple>();
    }

    void OnEnable()
    {
        if (meter != null) meter.Changed += OnMeterChanged;
        Refresh();
    }

    void OnDisable()
    {
        if (meter != null) meter.Changed -= OnMeterChanged;
    }

    void OnMeterChanged(float cur) => Refresh();
    void Refresh()
    {
        if (!label || meter == null) return;
        label.text = string.Format(format, System.Math.Round(meter.Current, decimals));
    }
}