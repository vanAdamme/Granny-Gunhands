public interface IDamageable
{
    /// Apply damage to this object.
    /// @param amount   How much damage to apply.
    /// @param attacker Optional: who caused the damage (player, projectile owner, etc.)
    void TakeDamage(float amount, UnityEngine.GameObject attacker = null);
}