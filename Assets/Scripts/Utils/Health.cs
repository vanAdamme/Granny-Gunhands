using UnityEngine;
using DamageNumbersPro;

/// Basic behaviour for tracking the health of an object.
public class Health : MonoBehaviour
{
    [SerializeField, Min(1f)] private float m_MaxHealth = 1f;
    [SerializeField] private float m_CurrentHealth;
    [SerializeField] private DamageNumber damageNumberPrefab;
    [SerializeField] private DamageFlash damageFlash;

    public event System.Action OnDied;

    protected bool m_IsInvulnerable;
    protected bool m_IsDead;

    // Properties
    public float MaxHealth { get => m_MaxHealth; set => m_MaxHealth = Mathf.Max(1f, value); }
    public float CurrentHealth => m_CurrentHealth;
    public bool IsInvulnerable { get => m_IsInvulnerable; set => m_IsInvulnerable = value; }

    protected virtual void Awake()
    {
        m_CurrentHealth = MaxHealth;

        if (!damageFlash)
        {
            if (!TryGetComponent(out damageFlash))
                damageFlash = GetComponentInChildren<DamageFlash>(includeInactive: true);
        }
    }

    private void OnValidate()
    {
        if (m_MaxHealth < 1f) m_MaxHealth = 1f;
        if (m_CurrentHealth > m_MaxHealth) m_CurrentHealth = m_MaxHealth;
    }

    /// Applies damage to this object.
    /// Pass the attacker GameObject if you want damage-to-charge, on-hit effects, etc.
    public virtual void TakeDamage(float amount, GameObject attacker = null)
    {
        if (m_IsDead || m_IsInvulnerable || amount <= 0f)
            return;

        // Clamp and compute actually-applied damage
        float before  = m_CurrentHealth;
        m_CurrentHealth = Mathf.Max(0f, m_CurrentHealth - amount);
        float applied = before - m_CurrentHealth;
        if (applied <= 0f) return;

        // Global damage event (lets specials charge off damage dealt)
        DamageEvents.RaiseDamaged(attacker, this, applied);

        // Visual feedback
        if (damageNumberPrefab) damageNumberPrefab.Spawn(transform.position, applied);
        if (damageFlash) damageFlash.CallDamageFlash();

        // Death
        if (m_CurrentHealth <= 0f)
        {
            m_CurrentHealth = 0f;
            Die();
        }
    }

    /// Heals the GameObject, up to the maximum value.
    public virtual void Heal(float amount)
    {
        if (m_IsDead || amount <= 0f) return;
        m_CurrentHealth = Mathf.Min(MaxHealth, m_CurrentHealth + amount);
    }

    /// Notify listeners and disable to prevent further interaction.
    protected virtual void Die()
    {
        if (m_IsDead) return;
        m_IsDead = true;
        OnDied?.Invoke();
        gameObject.SetActive(false);
    }

    // Compatibility helper for UI/other systems
    public bool IsHurt() => m_CurrentHealth < MaxHealth;
}