using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyContext : MonoBehaviour, IEnemyContext
{
    [Header("FSM Bits")]
    [SerializeField] private EnemyStateMachine fsm;           // same GO
    [SerializeField] private Animator animator;

    [Header("Strategies")]
    [SerializeField] private MonoBehaviour targetProviderSource; // ITargetProvider
    [SerializeField] private MonoBehaviour movementSource;       // IMovementStrategy
    [SerializeField] private MonoBehaviour attackSource;         // IAttackStrategy

    [Header("Tuning")]
    [SerializeField] private float attackRange = 1.25f;
    [SerializeField] private float repathInterval = 0.25f;

    public Transform Transform => transform;
    public float AttackRange => attackRange;
    public float RepathInterval => repathInterval;

    public ITargetProvider TargetProvider { get; private set; }
    public IMovementStrategy Movement { get; private set; }
    public IAttackStrategy Attack { get; private set; }

    public bool IsAlive => !_dead;
    public bool IsHurtLockedOut => Time.time < hurtUnlockAt;

    float hurtUnlockAt;
    bool _dead;

    void Reset()
    {
        fsm = GetComponent<EnemyStateMachine>();
        if (!animator) animator = GetComponentInChildren<Animator>();
    }

    void Awake()
    {
        TargetProvider = targetProviderSource as ITargetProvider;
        Movement       = movementSource       as IMovementStrategy;
        Attack         = attackSource         as IAttackStrategy;

        if (!fsm) fsm = GetComponent<EnemyStateMachine>();
        if (!animator) animator = GetComponentInChildren<Animator>();

        if (TargetProvider == null) Debug.LogError($"{name}: targetProvider must implement ITargetProvider");
        if (Movement == null)       Debug.LogError($"{name}: movementSource must implement IMovementStrategy");
        if (Attack == null)         Debug.LogError($"{name}: attackSource must implement IAttackStrategy");
    }

    public void SetHurtLock(float seconds) => hurtUnlockAt = Time.time + Mathf.Max(0, seconds);

    public void PlayAnim(string trigger) { if (animator) animator.SetTrigger(trigger); }

    public void LookAt(Vector2 worldPoint)
    {
        if (!animator) return;
        var dir = ((Vector2)worldPoint - (Vector2)transform.position).normalized;
        animator.SetFloat("moveX", dir.x);
        animator.SetFloat("moveY", dir.y);
    }

    public void OnDeath()
    {
        _dead = true;
        // Disable hitboxes or physics here if you want; corpse cleanup is handled by Enemy.Die()
    }
}