using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class BreakableView : MonoBehaviour
{
    [SerializeField] private BreakableDefinition definition;
    [SerializeField] private Health health; // optional; auto-find on Reset
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private bool setHealthFromDefinition = true;

    private float lastHealth = -1f;

    void Reset()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        health = GetComponent<Health>();
    }

    void Awake()
    {
        if (!spriteRenderer) spriteRenderer = GetComponent<SpriteRenderer>();
        if (!health) health = GetComponent<Health>();

        if (health && setHealthFromDefinition && definition)
            health.MaxHealth = Mathf.Max(1f, definition.maxHealth);

        // seed starting sprite (highest threshold)
        ApplyStageSprite();
        if (health) lastHealth = health.CurrentHealth;

        // death FX
        if (health) health.OnDied += HandleDeath;
    }

    void OnDestroy()
    {
        if (health) health.OnDied -= HandleDeath;
    }

    void Update()
    {
        if (!definition || !health) return;

        if (!Mathf.Approximately(lastHealth, health.CurrentHealth))
        {
            // on-hit feedback
            if (health.CurrentHealth < lastHealth)
            {
                if (definition.vfxOnHit)
                    VFX.Spawn(definition.vfxOnHit, transform.position, Quaternion.identity, definition.vfxOnHitLifetime);
                if (definition.sfxOnHit)
                    AudioSource.PlayClipAtPoint(definition.sfxOnHit, transform.position);
            }

            ApplyStageSprite();
            lastHealth = health.CurrentHealth;
        }
    }

    private void HandleDeath()
    {
        if (definition)
        {
            if (definition.vfxOnDeath)
                VFX.Spawn(definition.vfxOnDeath, transform.position, Quaternion.identity, definition.vfxOnDeathLifetime);
            if (definition.sfxOnDeath)
                AudioSource.PlayClipAtPoint(definition.sfxOnDeath, transform.position);
        }
    }

    private void ApplyStageSprite()
    {
        if (!definition || !spriteRenderer || !health) return;

        float ratio = Mathf.Clamp01(health.CurrentHealth / Mathf.Max(1f, health.MaxHealth));

        Sprite chosen = null;
        for (int i = 0; i < definition.stages.Length; i++)
        {
            var s = definition.stages[i];
            if (ratio <= s.threshold && s.sprite != null)
            {
                chosen = s.sprite;
                break;
            }
        }

        if (chosen) spriteRenderer.sprite = chosen;
    }
}