using UnityEngine;

[DisallowMultipleComponent]
public class SpawnDebrisOnDeath : MonoBehaviour
{
    [SerializeField] private Health health;

    [Header("Debris")]
    [Tooltip("Prefabs to scatter; each should have a SpriteRenderer. Rigidbody2D is optional (added if missing).")]
    [SerializeField] private GameObject[] debrisPrefabs;

    [Min(0)] public int minPieces = 3;
    [Min(0)] public int maxPieces = 6;

    [Header("Forces")]
    [SerializeField] private Vector2 speedRange = new Vector2(2f, 6f);     // m/s impulse
    [SerializeField] private Vector2 torqueRange = new Vector2(-90f, 90f); // deg/s impulse
    [SerializeField, Range(0f, 1f)] private float upwardBias = 0.25f;      // blend toward +Y

    [Header("Lifetime")]
    [SerializeField] private float pieceLifetime = 3f; // seconds; <=0 means don’t auto-destroy
    [SerializeField] private bool inheritSpriteTint = true;
    [SerializeField] private bool matchSortingLayer = true;

    [Header("Auto Fade (optional)")]
    [SerializeField] private bool autoAddFadeAndDestroy = true;
    [SerializeField] private float fadeDelay = 1.5f;
    [SerializeField] private float fadeDuration = 0.75f;
    [SerializeField] private bool fadeDestroyOnDisable = true;

    [Header("FX (optional)")]
    [SerializeField] private GameObject vfxBurst;
    [SerializeField] private float vfxLifetime = 1.2f;

    void Reset() => health = GetComponent<Health>();

    void Awake()
    {
        if (!health) health = GetComponent<Health>();
        if (health) health.OnDied += HandleDeath;
    }

    void OnDestroy()
    {
        if (health) health.OnDied -= HandleDeath;
    }

    private void HandleDeath()
    {
        if (vfxBurst) VFX.Spawn(vfxBurst, transform.position, Quaternion.identity, vfxLifetime);

        int count = Mathf.Clamp(Random.Range(minPieces, maxPieces + 1), 0, 128);
        if (count == 0 || debrisPrefabs == null || debrisPrefabs.Length == 0) return;

        var sourceSr = GetComponent<SpriteRenderer>();

        for (int i = 0; i < count; i++)
        {
            var prefab = debrisPrefabs[Random.Range(0, debrisPrefabs.Length)];
            if (!prefab) continue;

            // Randomized direction with slight upward bias
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            dir = Vector2.Lerp(dir, Vector2.up, upwardBias).normalized;

            // Spawn
            var p = Instantiate(prefab, transform.position, Quaternion.Euler(0f, 0f, Random.Range(0f, 360f)));

            // Ensure physics
            var rb = p.GetComponent<Rigidbody2D>();
            if (!rb) rb = p.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            // Kick it
            float speed = Random.Range(speedRange.x, speedRange.y);
            rb.AddForce(dir * speed, ForceMode2D.Impulse);
            rb.AddTorque(Random.Range(torqueRange.x, torqueRange.y) * Mathf.Deg2Rad, ForceMode2D.Impulse);

            // Visual inheritance and sorting
            if (p.TryGetComponent<SpriteRenderer>(out var pieceSr))
            {
                if (inheritSpriteTint && sourceSr) pieceSr.color = sourceSr.color;
                if (matchSortingLayer && sourceSr)
                {
                    pieceSr.sortingLayerID = sourceSr.sortingLayerID;
                    pieceSr.sortingOrder   = sourceSr.sortingOrder + 1;
                }
            }

            // Optional fade helper (added only if missing)
            if (autoAddFadeAndDestroy && !p.GetComponent<FadeAndDestroy>())
            {
                var fad = p.AddComponent<FadeAndDestroy>();
                // Configure safely via reflection-free, public fields
                // (assuming fields as provided in our FadeAndDestroy.cs)
                // If you renamed fields, adjust below.
                var fadType = typeof(FadeAndDestroy);
                fadType.GetField("delayBeforeFade", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)?.SetValue(fad, fadeDelay);
                fadType.GetField("fadeDuration",    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)?.SetValue(fad, fadeDuration);
                fadType.GetField("destroyOnDisable",System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)?.SetValue(fad, fadeDestroyOnDisable);
            }

            // Hard safety cleanup (kept even if fading is disabled)
            if (pieceLifetime > 0f) Destroy(p, pieceLifetime);
        }
    }
}