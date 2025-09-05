using UnityEngine;

/// <summary>
/// Base class for targets in the game, incorporating health and damage.
/// </summary>
public class Target : Health, IDamageable
{
    [Tooltip("Customize rate of damage for this target")]
    [SerializeField] private float m_DamageMultiplier = 1f;

    // New correct override: matches Health.TakeDamage(float, GameObject)
    public override void TakeDamage(float amount, GameObject attacker = null)
    {
        if (amount <= 0f) return;
        float scaled = amount * m_DamageMultiplier;
        base.TakeDamage(scaled, attacker);
        // additional per-target logic here if needed
    }

    // Compatibility overload for systems/interfaces that still call TakeDamage(float)
    // (If your IDamageable already includes the attacker, you can remove this.)
    public void TakeDamage(float amount)
    {
        TakeDamage(amount, null);
    }
}