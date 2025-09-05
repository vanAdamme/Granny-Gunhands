using UnityEngine;

public abstract class SpecialWeaponBase : MonoBehaviour
{
    [SerializeField] protected MonoBehaviour chargeSource; // SpecialChargeSimple on player
    protected ISpecialCharge charge;

    protected virtual void Awake()
    {
        charge = chargeSource as ISpecialCharge;
        if (charge == null)
        {
            // fallback – still using the modern API
            charge = Object.FindFirstObjectByType<SpecialChargeSimple>();
        }
    }

    public bool CanActivate => charge != null && charge.IsReady && CanActivateInternal();

    /// <summary>Extra checks like cooldown/resource/line-of-sight etc.</summary>
    protected virtual bool CanActivateInternal() => true;

    /// <summary>Public entry. Only call this if CanActivate is true.</summary>
    public void Activate()
    {
        if (!CanActivate) return;
        ActivateInternal();
        charge?.Consume();
        SpecialEvents.ReportFired();
    }

    /// <summary>Your concrete special effect goes here.</summary>
    protected abstract void ActivateInternal();
}