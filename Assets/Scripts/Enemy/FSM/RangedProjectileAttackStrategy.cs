using UnityEngine;

public sealed class RangedProjectileAttackStrategy : MonoBehaviour, IAttackStrategy
{
    [Header("Shooter hookup")]
    [SerializeField] private EnemyShooter shooter; // your existing firing component
    [SerializeField] private Transform muzzle;     // optional: where to aim from

    [Header("Timing")]
    [SerializeField] private float fireCooldown = 0.25f;
    [SerializeField] private bool requireLineOfSight = false;
    [SerializeField] private LayerMask losMask;

    float _cooldownLeft;

    void Reset()
    {
        if (!shooter) shooter = GetComponent<EnemyShooter>();
        if (!muzzle)  muzzle  = transform;
    }

    public bool TryAttack(IEnemyContext ctx, Transform target, float dt)
    {
        _cooldownLeft -= dt;
        if (_cooldownLeft > 0f || shooter == null || target == null) return false;

        // Optional simple LOS gate
        if (requireLineOfSight)
        {
            var from = muzzle ? muzzle.position : ctx.Transform.position;
            var to   = target.position;
            var dir  = (to - from).normalized;
            var dist = Vector2.Distance(from, to);

            if (Physics2D.Raycast(from, dir, dist, losMask))
                return false; // blocked
        }

        // Face target (context flips sprites already, but this keeps guns honest)
        ctx.LookAt(target.position);

        // Fire!
        // Assumes EnemyShooter exposes a method to shoot toward a world point.
        // If your API differs, adapt this call site only.
        shooter.FireAt(target.position);

        _cooldownLeft = fireCooldown;
        return true;
    }
}