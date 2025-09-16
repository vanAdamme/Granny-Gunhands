using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class SpawnFadeIn : MonoBehaviour
{
    [Header("Fade Settings")]
    [Tooltip("Total time in seconds for the fade-in.")]
    [SerializeField] private float duration = 0.35f;

    [Tooltip("Curve mapping 0..1 -> 0..1. 0 = start, 1 = end.")]
    [SerializeField] private AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Tooltip("If true, include all child SpriteRenderers.")]
    [SerializeField] private bool includeChildren = true;

    [Tooltip("If true, start from pure black; otherwise starts from the sprite's color but with alpha=0.")]
    [SerializeField] private bool fromBlack = true;

    [Tooltip("If true, use unscaled time (ignores Time.timeScale).")]
    [SerializeField] private bool useUnscaledTime = false;

    [Header("Optional")]
    [Tooltip("Disable these components while fading (e.g., colliders or AI scripts).")]
    [SerializeField] private Behaviour[] disableWhileFading;

    // Cache
    private readonly List<SpriteRenderer> _renderers = new List<SpriteRenderer>();
    private readonly List<Color> _targetColors = new List<Color>();
    private Coroutine _routine;

    /// <summary>Configure from code before Play() if you want.</summary>
    public void Configure(float duration, AnimationCurve curve, bool includeChildren, bool fromBlack, bool useUnscaledTime)
    {
        this.duration = duration;
        this.curve = curve ?? AnimationCurve.EaseInOut(0, 0, 1, 1);
        this.includeChildren = includeChildren;
        this.fromBlack = fromBlack;
        this.useUnscaledTime = useUnscaledTime;
    }

    public void Play()
    {
        if (_routine != null) StopCoroutine(_routine);
        CollectRenderers();
        _routine = StartCoroutine(FadeRoutine());
    }

    private void CollectRenderers()
    {
        _renderers.Clear();
        _targetColors.Clear();

        if (includeChildren)
            GetComponentsInChildren(_renderers);
        else
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr) _renderers.Add(sr);
        }

        _targetColors.Capacity = Mathf.Max(_targetColors.Capacity, _renderers.Count);
        foreach (var sr in _renderers)
        {
            _targetColors.Add(sr.color); // original target color per renderer
        }

        // Set start color (black or transparent)
        for (int i = 0; i < _renderers.Count; i++)
        {
            var sr = _renderers[i];
            var target = _targetColors[i];
            Color start;
            if (fromBlack)
            {
                // keep original alpha to avoid popping if sprite had custom alpha
                start = new Color(0f, 0f, 0f, target.a);
            }
            else
            {
                start = new Color(target.r, target.g, target.b, 0f);
            }
            sr.color = start;
        }
    }

    private IEnumerator FadeRoutine()
    {
        // Temporarily disable specified behaviours (optional)
        ToggleBehaviours(false);

        float t = 0f;
        float dur = Mathf.Max(0.0001f, duration);

        while (t < dur)
        {
            var raw = t / dur;
            var k = curve != null ? Mathf.Clamp01(curve.Evaluate(raw)) : raw;

            for (int i = 0; i < _renderers.Count; i++)
            {
                var sr = _renderers[i];
                if (!sr) continue;

                var target = _targetColors[i];

                if (fromBlack)
                {
                    // Lerp black->original colour (alpha stays at target alpha)
                    sr.color = Color.Lerp(new Color(0f, 0f, 0f, target.a), target, k);
                }
                else
                {
                    // Lerp transparent->original (true fade-in)
                    var c = target;
                    c.a = Mathf.Lerp(0f, target.a, k);
                    sr.color = c;
                }
            }

            t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            yield return null;
        }

        // Snap to final colours
        for (int i = 0; i < _renderers.Count; i++)
        {
            if (_renderers[i]) _renderers[i].color = _targetColors[i];
        }

        ToggleBehaviours(true);
        _routine = null;
    }

    private void ToggleBehaviours(bool enable)
    {
        if (disableWhileFading == null) return;
        foreach (var b in disableWhileFading)
        {
            if (b) b.enabled = enable;
        }
    }
}