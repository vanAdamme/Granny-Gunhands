using UnityEngine;

public abstract class SpecialWeaponBase : MonoBehaviour
{
    [Header("Special")]
    [SerializeField] protected Sprite icon;
    [SerializeField, Min(0.05f)] protected float cooldown = 0.5f;

    protected SpecialChargeMeter meter;
    float nextReadyAt;

    protected virtual void Awake()
    {
        meter = PlayerController.Instance.GetComponent<SpecialChargeMeter>();
        if (icon) UIController.Instance.UpdateSpecialWeaponIcon(icon); // hooks your UI icon
    }

    public bool TryActivate()
    {
        if (!meter || !meter.IsFull) return false;
        if (Time.time < nextReadyAt)  return false;

        if (ActivateSpecial())
        {
            meter.SpendFull();
            nextReadyAt = Time.time + cooldown;
            return true;
        }
        return false;
    }

    protected abstract bool ActivateSpecial();
}