using UnityEngine;

public abstract class Weapon : MonoBehaviour
{
    public enum FireMode { SemiAuto, FullAuto }

    [Header("Visuals")]
    [SerializeField] protected SpriteRenderer spriteRenderer;
    [SerializeField] public Sprite icon;

    [Header("Aiming")]
    [SerializeField] protected Transform muzzle;

    [Header("Firing")]
    [SerializeField] protected FireMode fireMode = FireMode.SemiAuto;
    public virtual float CooldownWindow { get; set; } = 0.15f;

    // Data wiring
    public WeaponDefinition Definition { get; protected set; }
    protected int currentLevel = 1;
    public int Level => currentLevel;
    protected WeaponDefinition.WeaponLevelData data;

    // cooldown gate
    protected float nextFireTime;

    // --------- Properties expected elsewhere ----------
    public virtual Transform Muzzle => muzzle;         // PlayerShooting expects this
    public virtual FireMode Mode    => fireMode;       // PlayerShooting expects this

    protected virtual void Awake()
    {
        if (!spriteRenderer) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    protected virtual void Update() => FlipSprite();

    protected void FlipSprite()
    {
        if (PlayerController.Instance == null) return;
        var scale = transform.localScale;
        if (transform.position.x > PlayerController.Instance.transform.position.x)
            scale.y = Mathf.Abs(scale.y);
        else
            scale.y = -Mathf.Abs(scale.y);
        transform.localScale = scale;
    }

    // Back-compat: some code may still call Fire(); we route to Shoot(muzzle forward)
    public virtual void Fire()
    {
        var dir = muzzle ? (Vector2)muzzle.right : Vector2.right;
        TryFire(dir);
    }

    /// <summary>Preferred entry point from input. Enforces cooldown.</summary>
    public virtual bool TryFire(Vector2 dir)
    {
        if (Time.time < nextFireTime) return false;
        Shoot(dir);
        nextFireTime = Time.time + Mathf.Max(0.01f, CooldownWindow);
        return true;
    }

    /// <summary>Child class actually spawns/launches projectiles.</summary>
    protected abstract void Shoot(Vector2 dir);

    // ----- Data binding / upgrades -----
    public virtual void SetDefinition(WeaponDefinition def) => SetDefinition(def, 1);

    public virtual void SetDefinition(WeaponDefinition def, int level)
    {
        if (!def) return;

        Definition = def;
        currentLevel = Mathf.Clamp(level, 1, (def.Levels?.Count ?? 1));
        data = def.GetLevelData(currentLevel - 1);

        icon = def.Icon;
        if (data != null)
        {
            if (data.spriteOverride && spriteRenderer) spriteRenderer.sprite = data.spriteOverride;
            CooldownWindow = data.cooldown;
        }
    }

    public virtual bool TryUpgrade()
    {
        if (!Definition || Definition.Levels == null) return false;
        int next = currentLevel + 1;
        if (next > Definition.Levels.Count) return false;
        SetDefinition(Definition, next);
        return true;
    }
}