using UnityEngine;

[DisallowMultipleComponent]
public class DamageableAdapter : MonoBehaviour, IDamageable
{
    [SerializeField] private Health health;

    void Reset() => health = GetComponent<Health>();

    public void TakeDamage(float amount, GameObject attacker = null)
    {
        if (!health) health = GetComponent<Health>();
        if (!health) return; // if you forgot to add Health, just no-op
        health.TakeDamage(amount, attacker);
    }

    // Optional legacy shim: if some old code still calls without attacker, keep this too.
    public void TakeDamage(float amount)
    {
        TakeDamage(amount, null);
    }
}