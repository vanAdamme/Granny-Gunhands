using UnityEngine;
using Pathfinding;

/// Move directly toward the player but "snake" left/right with a lateral sine.
/// Works with AIPath (preferred) and falls back to Rigidbody2D steering.
/// Add to only those enemies that should snake; wire it in EnemyContext → Movement Source.
[DisallowMultipleComponent]
public class WobblyRunAtPlayerStrategy : MonoBehaviour, IMovementStrategy
{
    public enum Side { Left, Right }

    [Header("A* (preferred)")]
    [SerializeField] private AIPath ai;                 // auto-found
    [SerializeField] private bool callSearchPath = true;
    [Tooltip("Throttle SearchPath calls; AIPath already auto-replans.")]
    [Min(0f)] public float minRepathInterval = 0.10f;

    [Header("Fallback (no A*)")]
    [SerializeField] private Rigidbody2D rb;            // auto-found
    [SerializeField] private float rbMoveSpeed = 3.5f;

    [Header("Snake (temporal sine)")]
    [Tooltip("Peak lateral offset in world units (before distance scaling/jitter).")]
    [Min(0f)] public float amplitude = 2.0f;

    [Tooltip("Oscillations per second (base frequency before jitter).")]
    [Min(0f)] public float frequency = 0.9f;

    [Tooltip("Smooth the offset changes (0 = raw, 1 = heavy smoothing).")]
    [Range(0f, 1f)] public float smooth = 0.20f;

    [Tooltip("Fade the wobble as we get close, so they can actually land hits.")]
    public bool scaleWithDistance = true;

    [Header("Randomisation (per enemy instance)")]
    [Tooltip("If true, pick Left/Right at runtime; otherwise use the field below.")]
    public bool randomiseSide = true;
    public Side side = Side.Right;

    [Tooltip("± jitter applied to amplitude per enemy (e.g., 0.2 = ±20%).")]
    [Range(0f, 1f)] public float amplitudeJitterPct = 0.25f;

    [Tooltip("± jitter applied to frequency per enemy (e.g., 0.15 = ±15%).")]
    [Range(0f, 1f)] public float frequencyJitterPct = 0.15f;

    [Header("Stop / Throttle")]
    [SerializeField] private float stopDistance = 0.2f;

    // internals
    Vector2 _smoothedOffset;
    float _phase;          // random per enemy
    float _ampMul = 1f;    // per-enemy amplitude multiplier
    float _freqMul = 1f;   // per-enemy frequency multiplier
    float _nextRepathAt;   // throttle SearchPath
    Vector3 _lastDest;     // destination cache to avoid needless writes

    const float TwoPi = Mathf.PI * 2f;
    const float DestEpsSqr = 0.001f;

    void Reset()
    {
        if (!ai) ai = GetComponent<AIPath>();
        if (!rb) rb = GetComponent<Rigidbody2D>();
    }

    void Awake()
    {
        ai = ai ? ai : (GetComponent<AIPath>() ?? GetComponentInParent<AIPath>());
        rb = rb ? rb : (GetComponent<Rigidbody2D>() ?? GetComponentInParent<Rigidbody2D>());

        if (!ai && !rb)
        {
            Debug.LogError($"{name}: {nameof(WobblyRunAtPlayerStrategy)} needs AIPath or Rigidbody2D.");
            enabled = false; return;
        }

        if (randomiseSide) side = (Random.value < 0.5f) ? Side.Left : Side.Right;

        // Per-enemy random phase and gentle jitter so they don't sync.
        _phase   = Random.value * TwoPi;
        _ampMul  = 1f + Random.Range(-amplitudeJitterPct,  amplitudeJitterPct);
        _freqMul = 1f + Random.Range(-frequencyJitterPct, frequencyJitterPct);

        // Keep AIPath speed sensible vs Enemy.MoveSpeed if present.
        var enemy = GetComponentInParent<Enemy>();
        if (ai && enemy) ai.maxSpeed = Mathf.Max(ai.maxSpeed, enemy.MoveSpeed);
    }

    public bool MoveTowards(IEnemyContext ctx, Vector2 destination, float dt)
    {
        // Use the nav agent's position when available (matches AIPath).
        var pos  = ai ? (Vector2)ai.position : (Vector2)ctx.Transform.position;
        var to   = destination - pos;
        var dist = to.magnitude;

        if (dist <= stopDistance)
        {
            if (rb) rb.linearVelocity = Vector2.zero;
            if (ai) ai.destination = destination;
            return false;
        }

        // Forward and perpendicular (screen-space 2D)
        Vector2 dir   = to / Mathf.Max(dist, 1e-4f);
        Vector2 right = new(-dir.y, dir.x);
        float   sign  = (side == Side.Right) ? 1f : -1f;

        // Temporal sine that crosses the centre line (true snake), with per-enemy jitter.
        float t      = Time.time;
        float omega  = TwoPi * Mathf.Max(0f, frequency) * _freqMul;
        float sine   = Mathf.Sin(t * omega + _phase);           // [-1, 1]
        float amp    = Mathf.Max(0f, amplitude) * _ampMul;

        if (scaleWithDistance)
        {
            // Grow wobble at range; taper within ~3*amp so they can connect.
            float approach = 3f * Mathf.Max(amp, 0.001f);
            amp *= Mathf.Clamp01(dist / approach);
        }

        float wave = Mathf.Sin((t + _phase) * TwoPi * frequency);
        var desiredOffset = right * (sign * amplitude * wave);

        // Smooth the offset to avoid harsh corners (frame-rate independent)
        float s = 1f - Mathf.Pow(1f - Mathf.Clamp01(smooth), dt * 60f);
        _smoothedOffset = Vector2.Lerp(_smoothedOffset, desiredOffset, s);

        var wobbleDestination = destination + _smoothedOffset;

#if UNITY_EDITOR
        // Visualisation: yellow = agent→player, cyan = player→wobble point, magenta = agent→wobble point.
        Debug.DrawLine(pos, destination, Color.yellow, 0f, false);
        Debug.DrawLine(destination, wobbleDestination, Color.cyan, 0f, false);
        Debug.DrawLine(pos, wobbleDestination, Color.magenta, 0f, false);
#endif

        // --- Drive movement (AIPath preferred) ---
        if (ai)
        {
            if ((_lastDest - (Vector3)wobbleDestination).sqrMagnitude > DestEpsSqr)
            {
                ai.destination = wobbleDestination;
                if (callSearchPath && Time.time >= _nextRepathAt)
                {
                    _nextRepathAt = Time.time + minRepathInterval;
                    ai.SearchPath();
                }
                _lastDest = ai.destination;
            }
            // Consider moving unless within stop window of current wobble point.
            return (pos - wobbleDestination).sqrMagnitude > (stopDistance * stopDistance);
        }

        // Rigidbody fallback (simple steering; no avoidance)
        var vel = (wobbleDestination - pos).normalized * rbMoveSpeed;
        rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, vel, 1f - Mathf.Exp(-10f * dt));
        return true;
    }

}