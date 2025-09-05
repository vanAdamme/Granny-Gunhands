using System;
using UnityEngine;

/// Global hub for damage notifications.
/// Emit exactly once from your central damage path (e.g., Health.TakeDamage).
public static class DamageEvents
{
    /// attacker: the GameObject that caused damage (projectile/weapon/owner)
    /// victim:   the Target/Health that got damaged
    /// amount:   raw damage amount actually applied (after reductions)
    public static event Action<GameObject, Component, float> Damaged;

    public static void RaiseDamaged(GameObject attacker, Component victim, float amount)
    {
        if (amount <= 0f) return;
        Damaged?.Invoke(attacker, victim, amount);
    }
}