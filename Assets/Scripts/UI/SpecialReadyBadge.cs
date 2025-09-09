using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SpecialReadyBadge : MonoBehaviour
{
    [Header("Optional explicit refs (leave empty; we’ll auto-find)")]
    [SerializeField] private SpecialWeaponBase special;      // equipped special on Player
    [SerializeField] private MonoBehaviour     chargeSource; // implements ISpecialCharge

    [Header("UI")]
    [SerializeField] private Image   icon;   // optional background ring
    [SerializeField] private Image   fill;   // set to Filled / Radial 360
    [SerializeField] private TMP_Text label;

    [Header("Visuals")]
    [SerializeField] private Color notReadyColor = new Color(1f, 1f, 1f, 0.35f);
    [SerializeField] private Color readyColor    = Color.white;
    [SerializeField] private bool  pulseWhenReady = true;
    [SerializeField, Min(0f)] private float pulseScale = 1.08f;
    [SerializeField, Min(0f)] private float pulseSpeed = 6f;

    private ISpecialCharge meter;
    private bool isReady;
    private bool subscribed;
    private Vector3 baseScale;
    const float EPS = 1e-5f;

    void Awake()
    {
        baseScale = transform.localScale;

        // Auto-wire children if empty
        if (!fill)  fill  = GetComponentInChildren<Image>(true);
        if (!label) label = GetComponentInChildren<TMP_Text>(true);
        if (!icon && fill) icon = fill;

        // Ensure fill shows as a radial ring
        if (fill)
        {
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Radial360;
            fill.fillOrigin = (int)Image.Origin360.Top;
        }
    }

    void OnEnable()
    {
        // Subscribe to player lifecycle so we bind once the player exists / changes
        if (GameSystems.Instance != null)
            GameSystems.Instance.PlayerChanged += OnPlayerChanged;

        // Try immediate resolve with the current player (if any)
        OnPlayerChanged(GameSystems.GetPlayer());
        Refresh();
    }

    void OnDisable()
    {
        if (GameSystems.Instance != null)
            GameSystems.Instance.PlayerChanged -= OnPlayerChanged;

        Unsubscribe();
        transform.localScale = baseScale;
    }

    void Update()
    {
        // Ready pulse
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

    // ---------- binding ----------

    private void OnPlayerChanged(PlayerController player)
    {
        Unsubscribe();

        special = ResolveSpecial(player);
        meter   = ResolveMeter(player);

        MaybeSubscribe();
        Refresh();
    }

    private SpecialWeaponBase ResolveSpecial(PlayerController player)
    {
        if (special) return special;

        // Prefer the player’s SpecialWeaponInput if present
        SpecialWeaponInput swi = player ? player.GetComponentInChildren<SpecialWeaponInput>(true) : null;
        if (!swi)
            swi = FindFirstObjectByType<SpecialWeaponInput>(FindObjectsInactive.Include);

        if (swi && swi.EquippedSpecial) return swi.EquippedSpecial;

        // Fallbacks: any special under player, or any in scene
        if (player)
        {
            var under = player.GetComponentInChildren<SpecialWeaponBase>(true);
            if (under) return under;
        }
        return FindFirstObjectByType<SpecialWeaponBase>(FindObjectsInactive.Include);
    }

    private ISpecialCharge ResolveMeter(PlayerController player)
    {
        // Respect explicit assignment first
        if (chargeSource is ISpecialCharge c1) return c1;

        // Prefer a meter on/under player
        if (player)
        {
            var c2 = player.GetComponentInChildren<SpecialChargeSimple>(true);
            if (c2) return c2;
        }

        // As a last resort, any MonoBehaviour that implements ISpecialCharge
        var mbs = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var mb in mbs) if (mb is ISpecialCharge c3) return c3;

        return null;
    }

    private void MaybeSubscribe()
    {
        if (subscribed) return;
        if (meter == null || special == null) return;

        meter.Changed     += OnMeterChanged;
        SpecialEvents.Fired += OnSpecialFired;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed) return;
        if (meter != null) meter.Changed -= OnMeterChanged;
        SpecialEvents.Fired -= OnSpecialFired;
        subscribed = false;
    }

    // ---------- UI ----------

    private void OnMeterChanged(float _) => Refresh();
    private void OnSpecialFired(float _)  => Refresh();

    private void Refresh()
    {
        if (meter == null || special == null)
        {
            SetVisuals(0f, false, 0f, 0f, unresolved:true);
            return;
        }

        float cur    = Mathf.Max(0f, meter.Current);
        float req    = Mathf.Max(EPS, special.Cost);
        float fill01 = Mathf.Clamp01(cur / req);
        bool  ready  = cur + EPS >= req;

        SetVisuals(fill01, ready, cur, req, unresolved:false);
    }

    private void SetVisuals(float fill01, bool ready, float cur, float req, bool unresolved)
    {
        isReady = ready;

        if (fill)  fill.fillAmount = fill01;
        if (icon)  icon.color      = ready ? readyColor : notReadyColor;

        if (label)
        {
            if (unresolved) label.text = "--";
            else if (ready) label.text = "READY";
            else            label.text = $"{Mathf.RoundToInt(cur)}/{Mathf.RoundToInt(req)}";
        }
    }
}