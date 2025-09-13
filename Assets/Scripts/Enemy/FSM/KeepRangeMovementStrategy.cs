using UnityEngine;
using Pathfinding;

/// <summary>
/// Kiting movement: approach until inside [FleeRange .. IdealMaxRange],
/// retreat if inside FleeRange, otherwise hold (with optional gentle strafe).
/// Works with AIPath if present; falls back to simple velocity.
/// </summary>
public class KeepRangeMovementStrategy : MonoBehaviour, IMovementStrategy
{
    [Header("Ranges (world units)")]
    [Tooltip("If farther than this, move closer.")]
    public float ApproachRange = 8f;
    [Tooltip("Preferred maximum range to shoot from.")]
    public float IdealMaxRange = 6f;
    [Tooltip("If closer than this, back away.")]
    public float FleeRange     = 2.5f;

    [Header("Motion")]
    [Tooltip("Used only for rigidbody fallback; AIPath controls its own speed.")]
    public float MoveSpeed = 3.5f;
    [Tooltip("Deadzone to reduce jitter at band edges.")]
    public float StopTolerance = 0.2f;

    [Header("Strafe (optional flair inside the band)")]
    public bool  StrafeInBand = true;
    public float StrafeRadius = 0.8f;
    public float StrafeSpeed  = 1.2f; // revs per second

    [Header("A* (optional)")]
    [SerializeField] private AIPath ai;
    [SerializeField] private bool callSearchPath = true;

    Rigidbody2D rb;
    float strafeTheta; // accumulated angle

    void Reset()
    {
        if (!ai) ai = GetComponent<AIPath>();
        rb = GetComponent<Rigidbody2D>();
    }

    void Awake()
    {
        if (!ai) ai = GetComponent<AIPath>();
        rb = rb ? rb : GetComponent<Rigidbody2D>();

        // Sync AIPath speed with Enemy if available (matches your AStarMovementStrategy)
        if (ai && TryGetComponent<Enemy>(out var enemy))
            ai.maxSpeed = Mathf.Max(ai.maxSpeed, enemy.MoveSpeed);
    }

    public bool MoveTowards(IEnemyContext ctx, Vector2 targetPos, float dt)
    {
        var myPos = (Vector2)ctx.Transform.position;
        var toT   = targetPos - myPos;
        var dist  = toT.magnitude;

        // Decide which band we’re in and pick a desired point
        Vector2 desiredPos;

        if (dist > ApproachRange + StopTolerance)
        {
            // Too far: move straight toward target
            desiredPos = targetPos;
        }
        else if (dist < FleeRange - StopTolerance)
        {
            // Too close: back away to outside flee band (a few meters behind)
            var dir = (dist > 1e-3f) ? (toT / dist) : Vector2.right;
            var backOff = Mathf.Max(IdealMaxRange * 0.5f, FleeRange + 1.0f);
            desiredPos = myPos - dir * backOff;
        }
        else
        {
            // In the sweet band: hold position (optionally strafe)
            desiredPos = myPos;

            if (StrafeInBand && dist > 1e-3f)
            {
                strafeTheta += (Mathf.PI * 2f * StrafeSpeed) * dt;
                var ortho = new Vector2(-toT.y, toT.x).normalized; // 90° around the target vector
                desiredPos = myPos + ortho * (Mathf.Sin(strafeTheta) * StrafeRadius);
            }
        }

        // Drive movement
        bool moving;
        if (ai)
        {
            if ((ai.destination - (Vector3)desiredPos).sqrMagnitude > 0.001f)
            {
                ai.destination = desiredPos;
                if (callSearchPath) ai.SearchPath();
            }
            // Consider "moving" if not inside a small stop window from desired point
            moving = (myPos - desiredPos).sqrMagnitude > (StopTolerance * StopTolerance);
        }
        else if (rb)
        {
            var toDesired = desiredPos - myPos;
            if (toDesired.magnitude <= StopTolerance)
            {
                rb.linearVelocity = Vector2.zero;
                moving = false;
            }
            else
            {
                var dir = toDesired.normalized;
                rb.linearVelocity = dir * MoveSpeed;
                moving = true;
            }
        }
        else
        {
            // No mover available; nothing to do
            moving = false;
        }

        // Let the sprite face the action (your context already flips sprites)
        ctx.LookAt(targetPos);

        return moving;
    }
}