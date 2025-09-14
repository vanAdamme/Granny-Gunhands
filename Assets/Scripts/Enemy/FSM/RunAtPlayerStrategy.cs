using UnityEngine;
using Pathfinding;

public class RunAtPlayerStrategy : MonoBehaviour, IMovementStrategy
{
    [Header("A* (preferred)")]
    [SerializeField] private AIPath ai;              // can be on the root; we auto-find in parents
    [SerializeField] private bool callSearchPath = true;

    [Header("Fallback (if no A*)")]
    [SerializeField] private Rigidbody2D rb;         // auto-find in parents
    [SerializeField] private float rbMoveSpeed = 3.5f;

    [Header("Tuning")]
    [SerializeField] private float stopDistance = 0.2f;

    // cache to avoid spamming SearchPath
    Vector3 _lastDest;
    const float DestEpsSqr = 0.001f;

    void Reset()
    {
        // Prefer same GO, then parent
        if (!ai) ai = GetComponent<AIPath>();
        if (!rb) rb = GetComponent<Rigidbody2D>();
    }

    void Awake()
    {
        // Allow strategies to live on the FSM child while movers live on the root
        if (!ai) ai = GetComponent<AIPath>() ?? GetComponentInParent<AIPath>();
        if (!rb) rb = GetComponent<Rigidbody2D>() ?? GetComponentInParent<Rigidbody2D>();

        // If we have AIPath, sync speed with Enemy (like AStarMovementStrategy does)
        if (ai)
        {
            var enemy = GetComponentInParent<Enemy>();
            if (enemy) ai.maxSpeed = Mathf.Max(ai.maxSpeed, enemy.MoveSpeed);

            // Make sure AIPath can actually move/search if those flags exist
            // (Some versions expose these; safe to ignore if not present)
            // ai.canMove = true; ai.canSearch = true;  // uncomment if you use these flags
        }
        else if (!rb)
        {
            Debug.LogError($"{name}: {nameof(RunAtPlayerStrategy)} needs AIPath or Rigidbody2D on this object or a parent.");
            enabled = false;
        }
    }

    public bool MoveTowards(IEnemyContext ctx, Vector2 destination, float dt)
    {
        // Prefer A* if present
        if (ai)
        {
            // Only push new destinations when they actually change
            if ((ai.destination - (Vector3)destination).sqrMagnitude > DestEpsSqr)
            {
                ai.destination = destination;
                if (callSearchPath) ai.SearchPath();
                _lastDest = ai.destination;
            }

            // “Moving” if we’re not within stop distance
            return Vector2.Distance(ctx.Transform.position, destination) > stopDistance;
        }

        // Fallback: RB2D kinematic pursuit (no avoidance)
        if (rb)
        {
            var pos = (Vector2)ctx.Transform.position;
            var to  = destination - pos;
            if (to.sqrMagnitude <= stopDistance * stopDistance)
            {
                rb.linearVelocity = Vector2.zero;
                return false;
            }

            rb.linearVelocity = to.normalized * rbMoveSpeed;
            return true;
        }

        return false; // no mover available (component disabled/missing)
    }
}