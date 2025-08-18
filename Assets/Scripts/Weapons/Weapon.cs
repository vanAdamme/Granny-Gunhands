using UnityEngine;

public abstract class Weapon : MonoBehaviour
{
    public enum FireMode { SemiAuto, FullAuto }

    [Header("General")]
    [SerializeField] protected SpriteRenderer spriteRenderer;
    [SerializeField] protected Transform muzzlePosition;   // make sure this points out of the barrel in +X
    [SerializeField] protected GameObject muzzleFlashPrefab;
    [SerializeField] public Sprite icon;
    [SerializeField] public bool autoFlip = true;

    [Header("Firing (shared)")]
    [SerializeField, Min(0.01f)] protected float cooldownWindow = 0.1f;
    [SerializeField] private FireMode fireMode = FireMode.FullAuto;
    public FireMode Mode => fireMode;
    protected float nextFire;
    public bool Ready => Time.time >= nextFire;

    public WeaponDefinition Definition { get; private set; }
    public void SetDefinition(WeaponDefinition def) { Definition = def; if (def && def.Icon) icon = def.Icon; }

    protected GameObject ownerRoot;
    public void SetOwner(GameObject root) => ownerRoot = root ? root : gameObject;

    public Transform Muzzle => muzzlePosition;

    protected virtual void Awake()
    {
        spriteRenderer ??= GetComponentInChildren<SpriteRenderer>();
        ownerRoot = transform.root.gameObject;
    }

    protected virtual void Update()
    {
        if (Pause.IsPaused) return;
        FlipSprite();
    }

    // Old signature kept for compatibility (ignored param):
    public bool TryFire(Vector2 _) => TryFireFromMuzzle();

    public bool TryFireFromMuzzle()
    {
        if (Time.time < nextFire) return false;

        DoMuzzleFlash();

        // ALWAYS use the muzzle’s facing
        Vector2 dir = muzzlePosition ? (Vector2)muzzlePosition.right : (Vector2)transform.right;

        Shoot(dir);
        nextFire = Time.time + cooldownWindow;
        return true;
    }

    protected abstract void Shoot(Vector2 dir);

    protected void DoMuzzleFlash()
    {
        if (!muzzleFlashPrefab || !muzzlePosition) return;
        var m = Instantiate(muzzleFlashPrefab, muzzlePosition.position, transform.rotation);
        Destroy(m, 0.05f);
    }

    public float CooldownWindow
    {
        get => cooldownWindow;
        set => cooldownWindow = Mathf.Max(0.01f, value);
    }

    protected void FlipSprite()
    {
        if (!autoFlip || !PlayerController.Instance) return;
        var scale = transform.localScale;
        if (transform.position.x > PlayerController.Instance.transform.position.x)
            scale.y = Mathf.Abs(scale.y);
        else
            scale.y = -Mathf.Abs(scale.y);
        transform.localScale = scale;
    }
}