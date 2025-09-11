using UnityEngine;

public class RangedAttackStrategy : MonoBehaviour, IAttackStrategy
{
    [Header("Shooter")]
    [SerializeField] private EnemyShooter shooter;        // assign your shooter here (same FSM GO or child)

    [Header("Timing")]
    [SerializeField, Min(0f)] private float fireInterval = 0.35f;

    [Header("Range")]
    [SerializeField, Min(0f)] private float minRange = 0f;
    [SerializeField, Min(0f)] private float maxRange = 12f;

    [Header("Line of Sight")]
    [Tooltip("Walls/obstacles. If empty (0), LOS is ignored.")]
    [SerializeField] private LayerMask losMask = 0;

    [Header("Aiming/Anim")]
    [SerializeField] private bool faceTarget = true;
    [SerializeField] private string shootAnimTrigger = ""; // leave blank to skip

    private float cooldown;

    public void OnEnter(IEnemyContext context)
    {
        if (!shooter) shooter = GetComponent<EnemyShooter>();
        if (shooter) shooter.SetControlledExternally(true);
        cooldown = 0f;
    }

    public bool TryAttack(IEnemyContext context, Transform target, float dt)
    {
        cooldown -= dt;
        if (!shooter || !target) return false;

        var origin   = transform.position;
        var toTarget = target.position - origin;
        var dist     = toTarget.magnitude;

        if (dist < minRange || dist > maxRange) return false;
        if (losMask.value != 0 && IsBlocked(origin, target.position)) return false;

        if (faceTarget && toTarget.sqrMagnitude > 1e-6f)
            context.LookAt(((Vector2)toTarget).normalized);

        if (cooldown > 0f) return false;

        shooter.FireAt(target.position);
        if (!string.IsNullOrEmpty(shootAnimTrigger))
            context.PlayAnim(shootAnimTrigger);

        cooldown = fireInterval;
        return true;
    }

    public void OnExit(IEnemyContext context)
    {
        if (shooter) shooter.SetControlledExternally(false);
    }

    private bool IsBlocked(Vector3 from, Vector3 to)
    {
        if (losMask.value == 0) return false;
        var dir = to - from;
        var len = dir.magnitude;
        if (len <= 1e-6f) return false;
        return Physics2D.Raycast(from, (Vector2)dir.normalized, len, losMask);
    }
}