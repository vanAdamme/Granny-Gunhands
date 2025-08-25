using UnityEngine;

[DisallowMultipleComponent]
public class DamageableAdapter : MonoBehaviour, IDamageable
{
    [SerializeField] private Health health;

    void Reset() => health = GetComponent<Health>();

    public void TakeDamage(float amount)
    {
        if (!health) health = GetComponent<Health>();
        if (!health) return; // Safe no-op if non-destructible config
        health.TakeDamage(amount);
    }
}