using UnityEngine;

[DisallowMultipleComponent]
public class EnemyStateMachine : MonoBehaviour
{
    [SerializeField] private MonoBehaviour contextSource; // must implement IEnemyContext
    private IEnemyContext ctx;

    private IEnemyState current;

    // ===== NEW: base state with Owner =====
    private abstract class StateBase : IEnemyState
    {
        protected EnemyStateMachine Owner;
        public void SetOwner(EnemyStateMachine owner) => Owner = owner;

        public abstract void OnEnter(IEnemyContext ctx);
        public abstract void OnExit(IEnemyContext ctx);
        public abstract void Tick(IEnemyContext ctx, float dt);
    }

    // Compose states
    private readonly IdleState idle       = new();
    private readonly MoveState moving     = new();
    private readonly AttackState attacking= new();
    private readonly HurtState hurt       = new();
    private readonly DeadState dead       = new();

    void Reset()
    {
        // makes inspector wiring harder to mess up
        if (!contextSource) contextSource = GetComponent<EnemyContext>();
    }

    void Awake()
    {
        ctx = contextSource as IEnemyContext;
        if (ctx == null)
        {
            Debug.LogError($"[{name}] EnemyStateMachine: contextSource must implement IEnemyContext.");
            enabled = false;
            return;
        }

        // wire owner into states once
        idle.SetOwner(this);
        moving.SetOwner(this);
        attacking.SetOwner(this);
        hurt.SetOwner(this);
        dead.SetOwner(this);
    }

    void OnEnable()
    {
        ChangeState(idle);
    }

    void Update()
    {
        current?.Tick(ctx, Time.deltaTime);
    }

    public void ChangeState(IEnemyState next)
    {
        if (current == next) return;
        current?.OnExit(ctx);
        current = next;

        // ensure Owner is set even if a brand-new state instance appears
        if (current is StateBase sb) sb.SetOwner(this);

        current?.OnEnter(ctx);
    }

    public void NotifyHurt(float iFramesSeconds = 0.15f)
    {
        if (!ctx.IsAlive) { ChangeState(dead); return; }
        ctx.SetHurtLock(iFramesSeconds);
        ChangeState(hurt);
    }

    public void NotifyDeath() => ChangeState(dead);

    // ===== States =====

    private class IdleState : StateBase
    {
        float checkTimer;

        public override void OnEnter(IEnemyContext ctx)
        {
            ctx.PlayAnim("Idle");
            checkTimer = 0f;
        }

        public override void OnExit(IEnemyContext ctx) { }

        public override void Tick(IEnemyContext ctx, float dt)
        {
            if (!ctx.IsAlive) return;

            checkTimer -= dt;
            if (checkTimer <= 0f)
            {
                checkTimer = 0.25f;
                if (ctx.TargetProvider.TryGetTarget(out var t))
                {
                    var dist = Vector2.Distance(ctx.Transform.position, t.position);
                    if (dist <= ctx.AttackRange) Owner.ChangeState(Owner.attacking);
                    else                         Owner.ChangeState(Owner.moving);
                }
            }
        }
    }

    private class MoveState : StateBase
    {
        float repathTimer;

        public override void OnEnter(IEnemyContext ctx)
        {
            ctx.PlayAnim("Move");
            repathTimer = 0f;
        }

        public override void OnExit(IEnemyContext ctx) { }

        public override void Tick(IEnemyContext ctx, float dt)
        {
            if (!ctx.IsAlive) return;

            if (ctx.TargetProvider.TryGetTarget(out var t))
            {
                ctx.LookAt(t.position);

                repathTimer -= dt;
                if (repathTimer <= 0f)
                {
                    repathTimer = ctx.RepathInterval;
                    // your strategy may repath internally; this keeps cadence intent clear
                }

                var stillMoving = ctx.Movement.MoveTowards(ctx, t.position, dt);

                var dist = Vector2.Distance(ctx.Transform.position, t.position);
                if (dist <= ctx.AttackRange) Owner.ChangeState(Owner.attacking);
                else if (!stillMoving && !ctx.TargetProvider.TryGetTarget(out _))
                    Owner.ChangeState(Owner.idle);
            }
            else
            {
                Owner.ChangeState(Owner.idle);
            }
        }
    }

    private class AttackState : StateBase
    {
        public override void OnEnter(IEnemyContext ctx) { ctx.PlayAnim("Attack"); ctx.Attack.OnEnter(ctx); }
        public override void OnExit(IEnemyContext ctx)  { ctx.Attack.OnExit(ctx); }

        public override void Tick(IEnemyContext ctx, float dt)
        {
            if (!ctx.TargetProvider.TryGetTarget(out var t)) { Owner.ChangeState(Owner.idle); return; }

            var dist = Vector2.Distance(ctx.Transform.position, t.position);
            ctx.LookAt(t.position);

            // Bail to Move if outside the preferred band in either direction.
            if (dist > ctx.AttackRange ||
                (ctx.Movement is IRangeAware pref && dist < pref.MinPreferredRange - 0.05f))
            {
                Owner.ChangeState(Owner.moving);
                return;
            }

            ctx.Attack.TryAttack(ctx, t, dt);
        }

    }

    private class HurtState : StateBase
    {
        public override void OnEnter(IEnemyContext ctx) => ctx.PlayAnim("Hurt");
        public override void OnExit(IEnemyContext ctx) { }

        public override void Tick(IEnemyContext ctx, float dt)
        {
            if (!ctx.IsAlive) { Owner.ChangeState(Owner.dead); return; }
            if (ctx.IsHurtLockedOut) return;

            if (ctx.TargetProvider.TryGetTarget(out var t))
            {
                var dist = Vector2.Distance(ctx.Transform.position, t.position);
                if (dist <= ctx.AttackRange) Owner.ChangeState(Owner.attacking);
                else                         Owner.ChangeState(Owner.moving);
            }
            else
            {
                Owner.ChangeState(Owner.idle);
            }
        }
    }

    private class DeadState : StateBase
    {
        bool handled;
        public override void OnEnter(IEnemyContext ctx)
        {
            if (handled) return;
            handled = true;
            ctx.PlayAnim("Die");
            ctx.OnDeath();
        }
        public override void OnExit(IEnemyContext ctx) { }
        public override void Tick(IEnemyContext ctx, float dt) { }
    }
}