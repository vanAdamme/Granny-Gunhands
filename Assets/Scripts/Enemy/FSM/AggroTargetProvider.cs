using UnityEngine;

/// Wraps another ITargetProvider (e.g., PlayerTargetProvider) and only
/// returns a target once aggro is earned (range or damage).
public class AggroTargetProvider : MonoBehaviour, ITargetProvider
{
    [Header("Inner provider (usually PlayerTargetProvider)")]
	[RequireInterface(typeof(ITargetProvider))]
    [SerializeField] private MonoBehaviour innerProviderSource; // must implement ITargetProvider
    private ITargetProvider inner;

    [Header("Aggro settings")]
    [SerializeField, Min(0f)] private float aggroRange = 6f;
    [SerializeField, Min(0f)] private float loseAggroRange = 12f;
    [SerializeField, Min(0f)] private float forgetAfter = 3f;

    [Header("Line of sight (optional)")]
    [SerializeField] private bool requireLOS = false;
    [SerializeField] private LayerMask losMask = 0;

    bool aggro;
    float lastAggroTime;
    Transform lastTarget;

    void OnValidate()
    {
        // Keep the cached interface in sync when editing in Inspector
        inner = innerProviderSource as ITargetProvider;
    }

    void Awake()
    {
        BindInner();
        if (inner == null)
            Debug.LogError($"{name}: AggroTargetProvider requires an inner ITargetProvider");
    }

private void BindInner()
    {
        // 1) Respect explicit Inspector assignment
        inner = innerProviderSource as ITargetProvider;
        if (inner != null) return;

        // 2) If there’s already some ITargetProvider on this GO, use it
        //    (e.g., CookieTargetProvider or PlayerTargetProvider)
        foreach (var mb in GetComponents<MonoBehaviour>())
        {
            if (mb == null) continue;
            if (mb == (MonoBehaviour)this) continue;
            if (mb is ITargetProvider itp)
            {
                innerProviderSource = mb; // serialize for clarity
                inner = itp;
                return;
            }
        }

        // 3) Nothing found? Add a PlayerTargetProvider and use that
        var p = gameObject.AddComponent<PlayerTargetProvider>();   // zero-config; it auto-binds GameSystems/Player
        innerProviderSource = p;
        inner = p;
#if UNITY_EDITOR
        if (!Application.isPlaying)
            UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
    
    public bool TryGetTarget(out Transform target)
    {
        target = null;
        if (inner == null) return false;
        if (!inner.TryGetTarget(out var candidate) || !candidate) return false;

        var me = transform.position;
        var dist = Vector2.Distance(me, candidate.position);

        // LOS check if enabled
        bool hasLOS = true;
        if (requireLOS && losMask.value != 0)
        {
            var dir = (candidate.position - me);
            hasLOS = !Physics2D.Raycast(me, dir.normalized, dir.magnitude, losMask);
        }

        // Acquire aggro
        if (!aggro && dist <= aggroRange && hasLOS)
        {
            aggro = true;
            lastAggroTime = Time.time;
            lastTarget = candidate;
        }

        // Maintain/lose aggro
        if (aggro)
        {
            bool tooFar = dist > loseAggroRange;
            bool expired = (Time.time - lastAggroTime) > forgetAfter;

            if (!tooFar && hasLOS)
            {
                lastAggroTime = Time.time;
                lastTarget = candidate;
            }

            if (!(tooFar && expired))
            {
                target = candidate;
                return true;
            }

            aggro = false;
            lastTarget = null;
        }

        return false;
    }

    // Called externally when damaged
    public void AggroFrom(Transform attacker)
    {
        if (!attacker) return;
        aggro = true;
        lastAggroTime = Time.time;
        lastTarget = attacker;
    }
}