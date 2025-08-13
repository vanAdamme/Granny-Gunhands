using UnityEngine;
using UnityEngine.Events;
using DamageNumbersPro;

/// <summary>
/// Basic behavior for tracking the health of an object.
/// </summary>
public class Health : MonoBehaviour
{
    [SerializeField, Min(1f)] private float m_MaxHealth = 1f;
    [SerializeField] float m_CurrentHealth;
    // [SerializeField] bool m_ResetOnStart;
    [SerializeField] private DamageNumber damageNumberPrefab;
    [SerializeField] private DamageFlash damageFlash;

    // [Tooltip("Notifies listeners that this object has died")]
    // public UnityEvent Died;

    // [Tooltip("Notifies listeners of updated health percentage")]
    // public UnityEvent<float> HealthChanged;

    protected bool m_IsInvulnerable;
    protected bool m_IsDead;

    // Properties
    public float MaxHealth { get => m_MaxHealth; set => m_MaxHealth = value; }
    public float CurrentHealth => m_CurrentHealth;
    public bool IsInvulnerable { get => m_IsInvulnerable; set => m_IsInvulnerable = value; }

    protected virtual void Awake()
    {
        m_CurrentHealth = MaxHealth;

        if (!damageFlash)
        {
            TryGetComponent(out damageFlash);
            if (!damageFlash)
                damageFlash = GetComponentInChildren<DamageFlash>(includeInactive: true);
        }
    }

    private void OnValidate()
    {
        if (m_MaxHealth < 1f) m_MaxHealth = 1f;
        if (m_CurrentHealth > m_MaxHealth) m_CurrentHealth = m_MaxHealth;
    }

    private void Start()
    {
        // HealthChanged.Invoke(CurrentHealth / MaxHealth);
    }

    // Applies damage to the GameObject.
    public virtual void TakeDamage(float amount)
    {
        // If already dead, do nothing
        if (m_IsDead || m_IsInvulnerable)
            return;

        m_CurrentHealth -= amount;

        // damage popup
        if (damageNumberPrefab != null)
        {
            DamageNumber damageNumber = damageNumberPrefab.Spawn(transform.position, amount);
        }
        // push enemy back
        // pushCounter = pushTime;

        // damage flash
        if (damageFlash != null) damageFlash.CallDamageFlash();

        // Check for death condition.
        if (m_CurrentHealth <= 0)
        {
            m_CurrentHealth = 0;
            Die();
        }

        // Notify listeners of the health change with the current health percentage.
        // HealthChanged.Invoke(CurrentHealth / MaxHealth); // Pass the current health percentage
    }

    // Heals the GameObject, up to the maximum value and notifies listeners
    public virtual void Heal(float amount)
    {
        // Don't heal if already dead
        if (m_IsDead)
            return;

        m_CurrentHealth += amount;

        if (m_CurrentHealth > MaxHealth)
            m_CurrentHealth = MaxHealth;

        // HealthChanged.Invoke(CurrentHealth / MaxHealth);
    }

    // Notify listeners that this object is dead and disable the GameObject to prevent further interaction.
    protected virtual void Die()
    {
        // Only die once
        if (m_IsDead)
            return;

        // DeathCroak();
        // DropItem();
        // PlayerController.Instance.GetExperience(experienceToGive);

        m_IsDead = true;
        // Died.Invoke();
        gameObject.SetActive(false);
    }

    // Compatibility helpers that UI/other systems might still call
    public bool IsHurt() => m_CurrentHealth < MaxHealth;
}