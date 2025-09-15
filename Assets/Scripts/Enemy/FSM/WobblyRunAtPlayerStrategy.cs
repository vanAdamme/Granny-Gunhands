using UnityEngine;
using Pathfinding;

/// Enemy movement that pursues a destination but adds a lateral wobble biased
/// to a single side (Left OR Right), suitable for A* (AIPath) or Rigidbody2D fallback.
/// Attach this component to enemies that should wobble; leave other enemies on your
/// regular movement strategy.
[DisallowMultipleComponent]
public class WobblyRunAtPlayerStrategy : MonoBehaviour, IMovementStrategy
{
    public enum Side { Left, Right }

    [Header("A* (preferred)")]
    [SerializeField] private AIPath ai;                 // auto-found on self/parents
    [SerializeField] private bool callSearchPath = true;

    [Header("Fallback (if no A*)")]
    [SerializeField] private Rigidbody2D rb;            // auto-found on self/parents
    [SerializeField] private float rbMoveSpeed = 3.5f;

    [Header("Wobble")]
    [Tooltip("Max lateral offset in world units when far from the destination.")]
    [Min(0f)] public float amplitude = 1.6f;

    [Tooltip("Oscillations per second for the wobble envelope.")]
    [Min(0f)] public float frequency = 0.8f;

    [Tooltip("Smooth the offset changes; 0 = raw, 1 = very smoothed.")]
    [Range(0f, 1f)] public float smooth = 0.25f;

    [Tooltip("Bias to a single side relative to forward-to-target.")]
    public Side side = Side.Right;

    [Tooltip("Fade wobble as we get close so enemies can reliably reach the target.")]
    public bool scaleWithDistance = true;

    [Header("Stop / Throttle")]
    [SerializeField] private float stopDistance = 0.2f;

    // internals
    private Vector2 _smoothedOffset;
    private float _phase;               // unique per instance for desync
    private const float TwoPi = Mathf.PI * 2f;

    // mirror RunAtPlayerStrategy: avoid spamming SearchPath
    private Vector3 _lastDest;
    private const float DestEpsSqr = 0.001f;

    void Reset()
    {
        if (!ai) ai = GetComponent<AIPath>();
        if (!rb) rb = GetComponent<Rigidbody2D>();
    }

    void Awake()
    {
        if (!ai) ai = GetComponent<AIPath>() ?? GetComponentInParent<AIPath>();
        if (!rb) rb = GetComponent<Rigidbody2D>() ?? GetComponentInParent<Rigidbody2D>();

        // Seed a unique phase so they don't all pulse together.
        _phase = Random.value * TwoPi;

        // If AIPath is present, keep speed sensible with your Enemy component (if any).
        if (ai)
        {
            var enemy = GetComponentInParent<Enemy>();
            if (enemy) ai.maxSpeed = Mathf.Max(ai.maxSpeed, enemy.MoveSpeed);
        }
        else if (!rb)
        {
            Debug.LogError($"{name}: {nameof(WobblyRunAtPlayerStrategy)} needs AIPath or Rigidbody2D on this object or a parent.");
            enabled = false;
        }
    }

public bool MoveTowards(IEnemyContext ctx, Vector2 destination, float dt)
{
    // Use the nav agent's position if present
    var pos  = ai ? (Vector2)ai.position : (Vector2)ctx.Transform.position;

    // --- HARD-CODED offset: 3 units to the chosen side ---
    // Build a 90°-right vector from agent->target
    var to   = destination - pos;
    var dist = to.magnitude;
    if (dist < 1e-4f) dist = 1e-4f;
    var dir   = to / dist;
    var right = new Vector2(-dir.y, dir.x);
    float sign = (side == Side.Right) ? 1f : -1f;

    // Constant, unsmoothed, unscaled wobble
    var offset = right * (sign * 3f);
    var wobbleDestination = destination + offset;

#if UNITY_EDITOR
    // Draw THREE lines so we can see everything:
    Debug.DrawLine(pos, destination, Color.yellow, 0f, false);          // agent -> player (raw)
    Debug.DrawLine(destination, wobbleDestination, Color.cyan, 0f, false); // player -> offset point
    Debug.DrawLine(pos, wobbleDestination, Color.magenta, 0f, false);   // agent -> wobble point (used)
#endif

    // Push destination exactly like RunAtPlayerStrategy
    if (ai)
    {
        ai.destination = wobbleDestination; // no caching, no threshold — force it
        if (callSearchPath) ai.SearchPath();
    }
    else if (rb)
    {
        rb.linearVelocity = (wobbleDestination - pos).normalized * rbMoveSpeed;
    }

#if UNITY_EDITOR
    // 1 log/second to confirm values at runtime
    _hb -= dt;
    if (_hb <= 0f)
    {
        _hb = 1f;
        Debug.Log($"[WobblyDbg id={GetInstanceID()}] pos={pos} destRaw={destination} offset={offset} wobDest={wobbleDestination} aiDest={(ai? ai.destination : (Vector3)Vector2.zero)}");
    }
#endif

    // Consider “moving” unless very close to the wobble point
    return (wobbleDestination - pos).sqrMagnitude > 0.04f; // 0.2^2
}

// add this field at the top of the class (inside #if UNITY_EDITOR guards if you like)
#if UNITY_EDITOR
float _hb;
#endif

}
