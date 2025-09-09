using UnityEngine;

[DisallowMultipleComponent]
public class EnemyStateMachine : MonoBehaviour
{
    [SerializeField] private MonoBehaviour contextSource; // assign a component that implements IEnemyContext
    private IEnemyContext ctx;

    private IEnemyState current;

    // States (compose, don’t inherit from MonoBehaviour)
    private readonly IdleState idle = new();
    private readonly MoveState moving = new();
    private readonly AttackState attacking = new();
    private readonly HurtState hurt = new();
    private readonly DeadState dead = new();

    // Cooldowns / timers that states can read via context if you prefer centralising
    private float timeSinceLastPath;

    void Awake()
    {
        ctx = contextSource as IEnemyContext;
        if (ctx == null)
        {
            Debug.LogError($"[{name}] EnemyStateMachine: contextSource must implement IEnemyContext.");
            enabled = false;
            return;
        }
    }

    void OnEnable()
    {
        ChangeState(idle);
    }

    void Update()
    {
        if (current == null) return;
        current.Tick(ctx, Time.deltaTime);
    }

    public void ChangeState(IEnemyState next)
    {
        if (current == next) return;
        current?.OnExit(ctx);
        current = next;
        current?.OnEnter(ctx);
    }

    // Public gateways for outside systems (HIT / DEATH hooks)
    public void NotifyHurt(float iFramesSeconds = 0.15f)
    {
        if (!ctx.IsAlive) { ChangeState(dead); return; }
        ctx.SetHurtLock(iFramesSeconds);
        ChangeState(hurt);
    }

    public void NotifyDeath()
    {
        ChangeState(dead);
    }

    // Simple state types
    private class IdleState : IEnemyState
    {
        float checkTimer;

        public void OnEnter(IEnemyContext ctx)
        {
            ctx.PlayAnim("Idle");
            checkTimer = 0f;
        }

        public void OnExit(IEnemyContext ctx) { }

        public void Tick(IEnemyContext ctx, float dt)
        {
            if (!ctx.IsAlive) return;

            checkTimer -= dt;
            if (checkTimer <= 0f)
            {
                checkTimer = 0.25f;
                if (ctx.TargetProvider.TryGetTarget(out var t))
                {
                    // Move or attack depending on range
                    var dist = Vector2.Distance(ctx.Transform.position, t.position);
                    if (dist <= ctx.AttackRange) ((EnemyStateMachine)ctx).ChangeState(((EnemyStateMachine)ctx).attacking);
                    else ((EnemyStateMachine)ctx).ChangeState(((EnemyStateMachine)ctx).moving);
                }
            }
        }
    }

    private class MoveState : IEnemyState
    {
        float repathTimer;

        public void OnEnter(IEnemyContext ctx)
        {
            ctx.PlayAnim("Move");
            repathTimer = 0f;
        }

        public void OnExit(IEnemyContext ctx) { }

        public void Tick(IEnemyContext ctx, float dt)
        {
            if (!ctx.IsAlive) return;
            if (ctx.TargetProvider.TryGetTarget(out var t))
            {
                // Face the target
                ctx.LookAt(t.position);

                // Periodically repath (avoids spamming A*)
                repathTimer -= dt;
                if (repathTimer <= 0f)
                {
                    repathTimer = ctx.RepathInterval;
                    // Movement strategy encapsulates A* details (destination = t.position)
                }
                var stillMoving = ctx.Movement.MoveTowards(ctx, t.position, dt);

                // If in range, swap to attack
                var dist = Vector2.Distance(ctx.Transform.position, t.position);
                if (dist <= ctx.AttackRange)
                    ((EnemyStateMachine)ctx).ChangeState(((EnemyStateMachine)ctx).attacking);
                // If no valid target anymore, go idle
                else if (!stillMoving && !ctx.TargetProvider.TryGetTarget(out _))
                    ((EnemyStateMachine)ctx).ChangeState(((EnemyStateMachine)ctx).idle);
            }
            else
            {
                // No target—idle
                ((EnemyStateMachine)ctx).ChangeState(((EnemyStateMachine)ctx).idle);
            }
        }
    }

    private class AttackState : IEnemyState
    {
        public void OnEnter(IEnemyContext ctx)
        {
            ctx.PlayAnim("AttackBlend"); // Or set an “isAttacking” bool in Animator
        }

        public void OnExit(IEnemyContext ctx) { }

        public void Tick(IEnemyContext ctx, float dt)
        {
            if (!ctx.IsAlive) return;
            if (!ctx.TargetProvider.TryGetTarget(out var t))
            {
                ((EnemyStateMachine)ctx).ChangeState(((EnemyStateMachine)ctx).idle);
                return;
            }

            var dist = Vector2.Distance(ctx.Transform.position, t.position);
            ctx.LookAt(t.position);

            if (dist > ctx.AttackRange)
            {
                ((EnemyStateMachine)ctx).ChangeState(((EnemyStateMachine)ctx).moving);
                return;
            }

            // Fire attack via strategy (handles cooldowns)
            var attacked = ctx.Attack.TryAttack(ctx, t, dt);
            // Stay in Attack; strategy manages fire-rate. If you prefer “attack -> idle/move” bounce, transition here.
        }
    }

    private class HurtState : IEnemyState
    {
        public void OnEnter(IEnemyContext ctx)
        {
            ctx.PlayAnim("Hurt");
        }

        public void OnExit(IEnemyContext ctx) { }

        public void Tick(IEnemyContext ctx, float dt)
        {
            if (!ctx.IsAlive)
            {
                ((EnemyStateMachine)ctx).ChangeState(((EnemyStateMachine)ctx).dead);
                return;
            }

            // If still in i-frames/lockout, remain; otherwise choose next intent
            if (ctx.IsHurtLockedOut) return;

            if (ctx.TargetProvider.TryGetTarget(out var t))
            {
                var dist = Vector2.Distance(ctx.Transform.position, t.position);
                if (dist <= ctx.AttackRange) ((EnemyStateMachine)ctx).ChangeState(((EnemyStateMachine)ctx).attacking);
                else ((EnemyStateMachine)ctx).ChangeState(((EnemyStateMachine)ctx).moving);
            }
            else
            {
                ((EnemyStateMachine)ctx).ChangeState(((EnemyStateMachine)ctx).idle);
            }
        }
    }

    private class DeadState : IEnemyState
    {
        bool handled;

        public void OnEnter(IEnemyContext ctx)
        {
            if (handled) return;
            handled = true;
            ctx.PlayAnim("Die");
            ctx.OnDeath();
            // Optional: disable colliders, AI, etc. here via ctx.OnDeath()
        }

        public void OnExit(IEnemyContext ctx) { }
        public void Tick(IEnemyContext ctx, float dt) { }
    }
}