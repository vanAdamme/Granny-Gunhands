using UnityEngine;
using UnityEngine.Events;
using System;
using DamageNumbersPro;

/// <summary>
/// Tracks health and death signalling. DOES NOT auto-disable on death anymore.
/// Subclasses decide when/how to remove the object (e.g., via animation).
/// </summary>
public class Health : MonoBehaviour
{
    [SerializeField, Min(1f)] private float m_MaxHealth = 1f;
    [SerializeField] float m_CurrentHealth;
    [SerializeField] private DamageNumber damageNumberPrefab;
    [SerializeField] private DamageFlash damageFlash;

    public event System.Action<float, GameObject> Damaged;
    public event System.Action OnDied;

    protected bool m_IsInvulnerable;
    protected bool m_IsDead;

    public float MaxHealth { get => m_MaxHealth; set => m_MaxHealth = value; }
    public float CurrentHealth => m_CurrentHealth;
    public bool IsInvulnerable { get => m_IsInvulnerable; set => m_IsInvulnerable = value; }

    protected virtual void Awake()
    {
        m_CurrentHealth = MaxHealth;

        if (!damageFlash)
        {
            TryGetComponent(out damageFlash);
            if (!damageFlash) damageFlash = GetComponentInChildren<DamageFlash>(includeInactive: true);
        }
    }

    private void OnValidate()
    {
        if (m_MaxHealth < 1f) m_MaxHealth = 1f;
        if (m_CurrentHealth > m_MaxHealth) m_CurrentHealth = m_MaxHealth;
    }

    public virtual void TakeDamage(float amount, GameObject attacker)
    {
        if (m_IsDead || m_IsInvulnerable) return;

        m_CurrentHealth -= amount;

        // NEW: fire typed event for listeners
        Damaged?.Invoke(amount, attacker);

        // Keep legacy SendMessage for now (backward-compat)
        // SendMessage("OnDamaged", new object[] { amount, attacker }, SendMessageOptions.DontRequireReceiver);

        if (damageNumberPrefab) damageNumberPrefab.Spawn(transform.position, amount);
        if (damageFlash) damageFlash.CallDamageFlash();

        if (m_CurrentHealth <= 0f)
        {
            m_CurrentHealth = 0f;
            Die();
        }
    }

    public virtual void Heal(float amount)
    {
        if (m_IsDead) return;
        m_CurrentHealth += amount;
        if (m_CurrentHealth > MaxHealth) m_CurrentHealth = MaxHealth;
    }

    /// <summary>
    /// Marks dead + notifies listeners. DOES NOT disable/destroy the GameObject.
    /// Subclasses should handle visuals/cleanup.
    /// </summary>
    protected virtual void Die()
    {
        if (m_IsDead) return;
        m_IsDead = true;
        OnDied?.Invoke();
        // Intentionally NOT disabling the GameObject anymore.
    }

    public bool IsHurt() => m_CurrentHealth < MaxHealth;
}