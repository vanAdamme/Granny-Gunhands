using UnityEngine;

[DisallowMultipleComponent]
public class ReturnToPoolAfterSeconds : MonoBehaviour
{
    [Tooltip("Seconds this object should stay alive. 0 or less = auto from particle duration (if present).")]
    [SerializeField] private float lifetime = 1.5f;

    [Tooltip("Use unscaled time so pause/slow-mo won't stretch the lifetime.")]
    [SerializeField] private bool useUnscaledTime = true;

    float elapsed;
    float resolvedLifetime;
    PooledObject pooled;

    void Awake()
    {
        pooled = GetComponent<PooledObject>();
    }

    void OnEnable()
    {
        elapsed = 0f;

        // Resolve lifetime once per activation
        resolvedLifetime = lifetime;

        // If lifetime not set, try ParticleSystem main.duration
        if (resolvedLifetime <= 0f)
        {
            var ps = GetComponentInChildren<ParticleSystem>(true);
            if (ps)
            {
                // If it loops, don't trust duration; fall back to a sane default
                var main = ps.main;
                resolvedLifetime = main.loop ? 1.5f : Mathf.Max(0.01f, main.duration + main.startLifetime.constantMax);
            }
            if (resolvedLifetime <= 0f) resolvedLifetime = 1.5f;
        }
    }

    void Update()
    {
        elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        if (elapsed >= resolvedLifetime)
            ReleaseOrDestroy();
    }

    void OnDisable()
    {
        // Be nice for pooled re-use
        elapsed = 0f;
    }

    void ReleaseOrDestroy()
    {
        if (pooled != null)
        {
            pooled.Release(); // should disable/deactivate immediately
        }
        else
        {
            // Safety fallback if this instance wasn’t spawned from a pool
            Destroy(gameObject);
        }
    }
}