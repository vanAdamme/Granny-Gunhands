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

    [Header("Facing (Sprite Flip Only)")]
    [SerializeField] private SpriteRenderer sprite;      // optional; auto-resolves if left empty
    [SerializeField] private bool flipByScale = true;    // true = scale.x flip; false = SpriteRenderer.flipX
    [SerializeField] private bool defaultFacingRight = true;

    private float baseScaleX = 1f;

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

        if (!sprite) sprite = GetComponentInChildren<SpriteRenderer>(includeInactive: true);
        baseScaleX = Mathf.Abs(transform.localScale.x);

        if (TargetProvider == null) Debug.LogError($"{name}: targetProvider must implement ITargetProvider");
        if (Movement == null)       Debug.LogError($"{name}: movementSource must implement IMovementStrategy");
        if (Attack == null)         Debug.LogError($"{name}: attackSource must implement IAttackStrategy");
    }

    public void SetHurtLock(float seconds) => hurtUnlockAt = Time.time + Mathf.Max(0, seconds);

    public void PlayAnim(string trigger)
    {
        if (!animator) return;
        if (HasTrigger(animator, trigger))
            animator.SetTrigger(trigger);
        // else: silently ignore (avoids spam when a trigger isn't present)
    }

    public void LookAt(Vector2 worldPoint)
    {
        // Sprite-flip only; no animator parameters
        var dx = worldPoint.x - transform.position.x;
        if (flipByScale)
        {
            var s = transform.localScale;
            // If default faces right, positive X means scale +baseScaleX; else invert
            var dir = dx >= 0f ? 1f : -1f;
            if (!defaultFacingRight) dir = -dir;
            s.x = baseScaleX * dir;
            transform.localScale = s;
        }
        else if (sprite)
        {
            // flipX = true usually means "face left" – invert if your art is opposite
            var faceLeft = dx < 0f;
            sprite.flipX = defaultFacingRight ? faceLeft : !faceLeft;
        }
    }

    public void OnDeath()
    {
        _dead = true;
        // Disable hitboxes or physics here if you want; corpse cleanup is handled by Enemy.Die()
    }

    static bool HasParam(Animator a, int hash)
    {
        if (!a) return false;
        foreach (var p in a.parameters)
            if (p.nameHash == hash) return true;
        return false;
    }

        static bool HasTrigger(Animator a, string name)
    {
        if (!a) return false;
        foreach (var p in a.parameters)
            if (p.type == AnimatorControllerParameterType.Trigger && p.name == name)
                return true;
        return false;
    }
}