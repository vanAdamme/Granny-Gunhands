using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class BreakableView : MonoBehaviour
{
    public enum ThresholdMode
    {
        /// <summary>Stage activates when healthRatio <= threshold (e.g., 0.75, 0.5, 0.25)</summary>
        AtOrBelow,
        /// <summary>Stage activates when healthRatio >= threshold (e.g., 1.0 full, 0.75 chipped, ...)</summary>
        AtOrAbove
    }

    [SerializeField] private BreakableDefinition definition;
    [SerializeField] private Health health;                  // optional; auto-find
    [SerializeField] private SpriteRenderer spriteRenderer;  // optional; auto-find
    [SerializeField] private bool setHealthFromDefinition = true;
    [SerializeField] private ThresholdMode thresholdMode = ThresholdMode.AtOrBelow;

#if UNITY_EDITOR
    [Header("Editor Preview")]
    [Tooltip("Preview which stage would be chosen at this health ratio (editor only).")]
    [Range(0f, 1f)] [SerializeField] private float previewHealthRatio = 1f;
#endif

    private float lastHealth = float.NaN;
    private Sprite initialSprite;

    void Reset()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        health = GetComponent<Health>();
    }

    void Awake()
    {
        if (!spriteRenderer) spriteRenderer = GetComponent<SpriteRenderer>();
        if (!health) health = GetComponent<Health>();
        if (spriteRenderer) initialSprite = spriteRenderer.sprite;

        // If we want the SO to define max HP, do it here
        if (health && setHealthFromDefinition && definition)
            health.MaxHealth = Mathf.Max(1f, definition.maxHealth);

        // Seed visuals
        ApplyStageSprite(CurrentRatio());
        lastHealth = health ? health.CurrentHealth : float.NaN;

        // Listen for death (for VFX/SFX); sprite before deactivation is still applied
        if (health) health.OnDied += HandleDeath;
    }

    void OnEnable()
    {
        // Re-apply on enable in case pooling restored old sprite/health
        ApplyStageSprite(CurrentRatio());
    }

    void OnDestroy()
    {
        if (health) health.OnDied -= HandleDeath;
    }

    void Update()
    {
        if (!health || !definition) return;

        float cur = health.CurrentHealth;
        if (!Mathf.Approximately(cur, lastHealth))
        {
            // Health changed → update sprite + on-hit feedback
            if (cur < lastHealth)
            {
                if (definition.vfxOnHit)
                    VFX.Spawn(definition.vfxOnHit, transform.position, Quaternion.identity, definition.vfxOnHitLifetime);
                if (definition.sfxOnHit)
                    AudioSource.PlayClipAtPoint(definition.sfxOnHit, transform.position);
            }

            ApplyStageSprite(CurrentRatio());
            lastHealth = cur;
        }
    }

    private void HandleDeath()
    {
        if (!definition) return;
        if (definition.vfxOnDeath)
            VFX.Spawn(definition.vfxOnDeath, transform.position, Quaternion.identity, definition.vfxOnDeathLifetime);
        if (definition.sfxOnDeath)
            AudioSource.PlayClipAtPoint(definition.sfxOnDeath, transform.position);
    }

    private float CurrentRatio()
    {
        if (!health) return 1f;
        float max = Mathf.Max(1f, health.MaxHealth);
        return Mathf.Clamp01(health.CurrentHealth / max);
    }

    private void ApplyStageSprite(float ratio)
    {
        if (!spriteRenderer) return;

        // No definition? Fall back to the initial sprite.
        if (!definition || definition.stages == null || definition.stages.Length == 0)
        {
            spriteRenderer.sprite = initialSprite;
            return;
        }

        // Choose the best-matching stage
        Sprite chosen = PickStageSprite(ratio);

        // If no stage matched, keep current if set; otherwise fallback to initial
        if (!chosen) chosen = spriteRenderer.sprite ? spriteRenderer.sprite : initialSprite;

        if (spriteRenderer.sprite != chosen) spriteRenderer.sprite = chosen;
    }

    private Sprite PickStageSprite(float ratio)
    {
        var stages = definition.stages;
        if (stages == null || stages.Length == 0) return null;

        // We’ll iterate once and keep track of the “closest” valid stage under the chosen mode.
        Sprite best = null;
        float bestKey = thresholdMode == ThresholdMode.AtOrBelow ? -1f : 2f;

        for (int i = 0; i < stages.Length; i++)
        {
            var s = stages[i];
            if (!s.sprite) continue;

            if (thresholdMode == ThresholdMode.AtOrBelow)
            {
                // want the HIGHEST threshold that is >= ratio? No: AtOrBelow means become active when ratio <= threshold.
                // So valid if ratio <= s.threshold; among valids, pick the LOWEST threshold (closest to ratio).
                if (ratio <= s.threshold)
                {
                    if (best == null || s.threshold < bestKey)
                    {
                        best = s.sprite;
                        bestKey = s.threshold;
                    }
                }
            }
            else // AtOrAbove
            {
                // valid if ratio >= s.threshold; among valids, pick the HIGHEST threshold (closest to ratio)
                if (ratio >= s.threshold)
                {
                    if (best == null || s.threshold > bestKey)
                    {
                        best = s.sprite;
                        bestKey = s.threshold;
                    }
                }
            }
        }
        return best;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // Ensure references
        if (!spriteRenderer) spriteRenderer = GetComponent<SpriteRenderer>();
        if (!health) health = GetComponent<Health>();

        // Sort stages into a sane order when editing, so designers don’t have to.
        if (definition != null && definition.stages != null && definition.stages.Length > 1)
        {
            // For AtOrBelow, we usually want thresholds high→low (0.75, 0.5, 0.25)
            // For AtOrAbove, we usually want low→high (0.0, 0.25, 0.5, 0.75, 1.0)
            System.Array.Sort(definition.stages, (a, b) =>
                thresholdMode == ThresholdMode.AtOrBelow
                    ? b.threshold.CompareTo(a.threshold)  // desc
                    : a.threshold.CompareTo(b.threshold)  // asc
            );
        }

        // Live preview in editor
        if (!Application.isPlaying)
            ApplyStageSprite(previewHealthRatio);
    }
#endif
}