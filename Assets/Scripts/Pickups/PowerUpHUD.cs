using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PowerUpHUD : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI timerText;

    private PowerUpController controller;
    private PowerUpDefinition current;
    private float remaining = -1f;

    private void Start()
    {
        var player = FindFirstObjectByType<PowerUpController>();
        if (!player) { gameObject.SetActive(false); return; }

        controller = player;
        controller.OnPowerUpStarted += HandleStarted;
        controller.OnPowerUpRefreshed += HandleRefreshed;
        controller.OnPowerUpEnded += HandleEnded;

        Hide();
    }

    private void Update()
    {
        if (current == null || remaining < 0f) return;

        remaining = Mathf.Max(0f, remaining - Time.deltaTime);
        timerText.text = $"{remaining:0.0}s";

        if (remaining <= 0f)
            Hide();
    }

    private void HandleStarted(PowerUpDefinition def, float rem)
    {
        current = def;
        remaining = rem;
        iconImage.sprite = def ? def.Icon : null;
        iconImage.enabled = true;
        timerText.enabled = def.durationSeconds > 0f;
        timerText.text = def.durationSeconds > 0f ? $"{rem:0.0}s" : "";
        gameObject.SetActive(true);
    }

    private void HandleRefreshed(PowerUpDefinition def, float rem)
    {
        if (current != def) return;
        remaining = rem;
        timerText.text = $"{rem:0.0}s";
    }

    private void HandleEnded(PowerUpDefinition def)
    {
        if (current != def) return;
        Hide();
    }

    private void Hide()
    {
        current = null;
        remaining = -1f;
        iconImage.enabled = false;
        timerText.enabled = false;
        gameObject.SetActive(false);
    }
}