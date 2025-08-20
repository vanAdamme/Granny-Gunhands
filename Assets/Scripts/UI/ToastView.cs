using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class ToastView : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private CanvasGroup group;
    [SerializeField] private Image background;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private RectTransform root;

    [Header("Animation")]
    [SerializeField] private float fadeIn = 0.15f;
    [SerializeField] private float hold = 1.6f;
    [SerializeField] private float fadeOut = 0.25f;
    [SerializeField] private float risePixels = 16f;

    public float Height => root ? root.rect.height : 32f;

    public void Show(string message, Sprite icon, Color tint, float duration, Action onFinished)
    {
        if (!group) group = GetComponent<CanvasGroup>();
        if (!root)  root  = transform as RectTransform;

        messageText.text = message;
        if (iconImage) { iconImage.sprite = icon; iconImage.enabled = icon != null; }
        if (background) background.color = new Color(tint.r, tint.g, tint.b, background.color.a);

        StopAllCoroutines();
        StartCoroutine(Run(duration > 0 ? duration : (fadeIn + hold + fadeOut), onFinished));
    }

    public void SetStackOffset(float y)
    {
        if (!root) root = transform as RectTransform;
        var pos = root.anchoredPosition;
        pos.y = -y; // stack downward
        root.anchoredPosition = pos;
    }

    private IEnumerator Run(float totalDuration, Action onFinished)
    {
        float animRise = risePixels;
        Vector2 startPos = root.anchoredPosition;

        // Fade in
        group.alpha = 0f;
        float t = 0f;
        while (t < fadeIn)
        {
            t += Time.unscaledDeltaTime;
            float n = Mathf.Clamp01(t / fadeIn);
            group.alpha = n;
            root.anchoredPosition = startPos + new Vector2(0, Mathf.Lerp(animRise, 0, n));
            yield return null;
        }
        group.alpha = 1f; root.anchoredPosition = startPos;

        // Hold
        float remain = Mathf.Max(0f, totalDuration - fadeIn - fadeOut);
        float elapsed = 0f;
        while (elapsed < remain)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        // Fade out
        t = 0f;
        while (t < fadeOut)
        {
            t += Time.unscaledDeltaTime;
            float n = Mathf.Clamp01(t / fadeOut);
            group.alpha = 1f - n;
            root.anchoredPosition = startPos + new Vector2(0, Mathf.Lerp(0, animRise, n));
            yield return null;
        }
        group.alpha = 0f;

        onFinished?.Invoke();
    }
}