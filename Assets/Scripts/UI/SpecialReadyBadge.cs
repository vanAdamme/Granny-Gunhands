using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SpecialReadyBadge : MonoBehaviour
{
    [Header("Optional explicit refs (leave empty; we’ll auto-find)")]
    [SerializeField] private SpecialWeaponBase special;      // equipped special on Player
    [SerializeField] private MonoBehaviour chargeSource;     // SpecialChargeSimple (ISpecialCharge)

    [Header("UI")]
    [SerializeField] private Image icon;   // optional background ring
    [SerializeField] private Image fill;   // set to Filled / Radial 360
    [SerializeField] private TMP_Text label;

    [Header("Visuals")]
    [SerializeField] private Color notReadyColor = new Color(1f, 1f, 1f, 0.35f);
    [SerializeField] private Color readyColor    = Color.white;
    [SerializeField] private bool  pulseWhenReady = true;
    [SerializeField, Min(0f)] private float pulseScale = 1.08f;
    [SerializeField, Min(0f)] private float pulseSpeed = 6f;

    [Header("Auto-resolve")]
    [SerializeField] private bool keepResolvingUntilFound = true;
    [SerializeField, Min(0.05f)] private float resolveEvery = 0.5f;
    [SerializeField] private bool logResolve = true;

    private ISpecialCharge meter;
    private bool isReady;
    private bool subscribed;
    private Vector3 baseScale;
    private float nextResolveAt;
    const float EPS = 1e-5f;

    void Awake()
    {
        baseScale = transform.localScale;

        // Auto-wire children if empty
        if (!fill)  fill  = GetComponentInChildren<Image>(true);
        if (!label) label = GetComponentInChildren<TMP_Text>(true);
        if (!icon && fill) icon = fill;

        // Ensure fill actually shows as a ring
        if (fill)
        {
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Radial360;
            fill.fillOrigin = (int)Image.Origin360.Top;
        }

        TryResolveRefs(force:true);
    }

    void OnEnable()
    {
        MaybeSubscribe();
        Refresh();
    }

    void OnDisable()
    {
        Unsubscribe();
        transform.localScale = baseScale;
    }

    void Update()
    {
        // Keep trying to bind until we have both special + meter
        if (keepResolvingUntilFound && (!special || meter == null) && Time.unscaledTime >= nextResolveAt)
        {
            if (TryResolveRefs(force:false))
                MaybeSubscribe();

            nextResolveAt = Time.unscaledTime + resolveEvery;
        }

        // Pulse when ready
        if (pulseWhenReady && isReady)
        {
            float s = 1f + (pulseScale - 1f) * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * pulseSpeed));
            transform.localScale = baseScale * s;
        }
        else
        {
            transform.localScale = Vector3.Lerp(transform.localScale, baseScale, Time.unscaledDeltaTime * 12f);
        }
    }

    bool TryResolveRefs(bool force)
    {
        bool changed = false;

        // Strong anchor: the Player
        var player = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);

        // 1) SPECIAL — ask SpecialWeaponInput first
        if (special == null || force)
        {
            SpecialWeaponBase found = null;

            SpecialWeaponInput swi = null;
            if (player) swi = player.GetComponentInChildren<SpecialWeaponInput>(true);
            if (!swi)   swi = FindFirstObjectByType<SpecialWeaponInput>(FindObjectsInactive.Include);

            if (swi && swi.EquippedSpecial != null)
                found = swi.EquippedSpecial;

            // Fallbacks if the input isn’t present
            if (!found && player) found = player.GetComponentInChildren<SpecialWeaponBase>(true);
            if (!found)           found = FindFirstObjectByType<SpecialWeaponBase>(FindObjectsInactive.Include);

            if (found != special)
            {
                special = found;
                changed = true;
                if (logResolve) Debug.Log($"[SpecialReadyBadge] special={(special ? special.name : "null")} cost={(special ? special.Cost : 0f)}");
            }
        }

        // 2) METER
        if (meter == null || force)
        {
            ISpecialCharge found = chargeSource as ISpecialCharge;

            if (found == null && player)
                found = player.GetComponentInChildren<SpecialChargeSimple>(true);

            if (found == null)
            {
                // Fallback: any MonoBehaviour that implements ISpecialCharge
                var mbs = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var mb in mbs) { if (mb is ISpecialCharge c) { found = c; break; } }
            }

            if (!ReferenceEquals(found, meter))
            {
                meter = found;
                changed = true;
                if (logResolve)
                {
                    var comp = meter as Component;
                    Debug.Log($"[SpecialReadyBadge] meter={(comp ? comp.name : "null")} current={(meter != null ? meter.Current : 0f)}");
                }
            }
        }

        return changed;
    }

    void MaybeSubscribe()
    {
        if (subscribed) return;
        if (meter == null || special == null) return;

        meter.Changed += OnMeterChanged;
        SpecialEvents.Fired += OnSpecialFired;
        subscribed = true;

        if (logResolve)
            Debug.Log($"[SpecialReadyBadge] Subscribed. cost={special.Cost}, current={meter.Current}");

        Refresh();
    }

    void Unsubscribe()
    {
        if (!subscribed) return;
        if (meter != null) meter.Changed -= OnMeterChanged;
        SpecialEvents.Fired -= OnSpecialFired;
        subscribed = false;
    }

    void OnMeterChanged(float _) => Refresh();
    void OnSpecialFired(float _)  => Refresh();

    void Refresh()
    {
        if (meter == null || special == null)
        {
            SetVisuals(0f, false, 0f, 0f, unresolved:true);
            return;
        }

        float cur = Mathf.Max(0f, meter.Current);
        float req = Mathf.Max(EPS, special.Cost);
        float fill01 = Mathf.Clamp01(cur / req);
        bool ready = cur + EPS >= req;

        SetVisuals(fill01, ready, cur, req, unresolved:false);
    }

    void SetVisuals(float fill01, bool ready, float cur, float req, bool unresolved)
    {
        isReady = ready;

        if (fill)  fill.fillAmount = fill01;
        if (icon)  icon.color      = ready ? readyColor : notReadyColor;

        if (label)
        {
            if (unresolved)      label.text = "--";
            else if (ready)      label.text = "READY";
            else                 label.text = $"{Mathf.RoundToInt(cur)}/{Mathf.RoundToInt(req)}";
        }
    }
}