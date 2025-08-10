using UnityEngine;

/// <summary>
/// Base class for targets in the game, incorporating health and damage.
/// </summary>
public class Target : Health, IDamageable
{
    [Tooltip("Customize rate of damage for this target")]
    [SerializeField] float m_DamageMultiplier = 1f;

    public override void TakeDamage(float amount)
    {
        float damage = amount * m_DamageMultiplier;
        base.TakeDamage(damage);

        // Customize any additional class-specific logic here
        // Debug.Log($"Target custom TakeDamage: {amount}");
    }
}