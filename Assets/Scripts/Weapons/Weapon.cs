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

    // Data wiring (flat definition model)
    public WeaponDefinition Definition { get; protected set; }
    protected int currentLevel = 1;
    public int Level => currentLevel;

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

    // Back-compat: some code may still call Fire(); route to Shoot(muzzle forward)
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

        Definition   = def;
        currentLevel = Mathf.Max(1, level);

        // runtime icon
        icon = def.GetIconForLevel(currentLevel);

        // base cooldown source
        CooldownWindow = Mathf.Max(0.01f, def.baseCooldown);

        // If you have per-level sprite overrides later, apply them here to spriteRenderer.
        // (We removed data-driven 'WeaponLevelData' entirely.)
    }

    /// <summary>
    /// Legacy hook used by some systems. With the flat definition model there’s
    /// no generic base upgrade; specific weapons (e.g., GenericProjectileWeapon)
    /// should implement IUpgradableWeapon and handle upgrades there.
    /// </summary>
    public virtual bool TryUpgrade() => false;
}