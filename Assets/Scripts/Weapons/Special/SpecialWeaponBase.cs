using UnityEngine;

public abstract class SpecialWeaponBase : MonoBehaviour
{
    [Header("Charge Source (optional)")]
    [Tooltip("Leave empty on prefabs; we’ll auto-find the player's meter at runtime.")]
    [SerializeField] protected MonoBehaviour chargeSource; // SpecialChargeSimple on player
    protected ISpecialCharge charge;

    [Header("Special Cost")]
    [Tooltip("Damage required in the shared meter to activate this special.")]
    [SerializeField, Min(0f)] private float requiredDamage = 50f;
    protected virtual float RequiredDamage => requiredDamage;

    const float EPS = 1e-5f;

    protected virtual void Awake()
    {
        ResolveCharge();
    }

    // Lazy resolve so prefabs work without scene refs and timing issues.
    protected bool ResolveCharge()
    {
        if (charge != null) return true;

        // 1) Explicit field if you set it
        charge = chargeSource as ISpecialCharge;
        if (charge != null) return true;

        // 2) Prefer the Player this special is attached to
        var local = GetComponentInParent<SpecialChargeSimple>(true);
        if (local != null) { charge = local; return true; }

        // 3) Last-ditch: global find (only if there is truly one in scene)
        charge = Object.FindFirstObjectByType<SpecialChargeSimple>();
        return charge != null;
    }

    public bool CanActivate =>
        ResolveCharge() &&
        charge.Current + EPS >= RequiredDamage &&
        CanActivateInternal();

    /// Override for cooldown/LOS/etc.
    protected virtual bool CanActivateInternal() => true;

    public void Activate()
    {
        if (!ResolveCharge()) { Debug.LogWarning($"{name}: No charge meter found."); return; }
        if (!CanActivate) return;

        // Spend first; if somehow it fails, bail safely
        Debug.Log($"[Special] Trying to spend {RequiredDamage}, meter={charge.Current}");

        if (!charge.TryConsume(RequiredDamage)) return;

        ActivateInternal();

        // Let UI/logic know a special fired (with the cost spent)
        SpecialEvents.ReportFired(RequiredDamage);
    }

    /// Your concrete effect goes here.
    protected abstract void ActivateInternal();
}