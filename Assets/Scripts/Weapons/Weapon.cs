using UnityEngine;

public abstract class Weapon : MonoBehaviour
{
	public enum FireMode { SemiAuto, FullAuto }

    [Header("General")]
    [SerializeField] protected SpriteRenderer spriteRenderer;
    [SerializeField] protected Transform muzzlePosition;
    [SerializeField] protected GameObject muzzleFlashPrefab;
    [SerializeField] public Sprite icon;

    [Header("Firing (shared)")]
    [SerializeField, Min(0.01f)] protected float cooldownWindow = 0.1f;
	[SerializeField] private FireMode fireMode = FireMode.FullAuto;
    protected float nextFire;

    protected GameObject ownerRoot;

	public FireMode Mode => fireMode;

	// Expose muzzle so handlers can aim from the correct point
	public Transform Muzzle => muzzlePosition;

    protected virtual void Awake()
    {
        spriteRenderer ??= GetComponentInChildren<SpriteRenderer>();
        ownerRoot = transform.root.gameObject;
    }

    protected virtual void Update()
    {
        FlipSprite();
    }

    // Called by handlers/AI. Handles cooldown + VFX and delegates to child.
    public bool TryFire(Vector2 direction)
    {
        if (Time.time < nextFire) return false;

        DoMuzzleFlash();
        Shoot(direction.sqrMagnitude > 0f ? direction.normalized : Vector2.right);
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

    protected void FlipSprite()
    {
        if (!PlayerController.Instance) return;
        var scale = transform.localScale;
        if (transform.position.x > PlayerController.Instance.transform.position.x)
            scale.y = Mathf.Abs(scale.y);
        else
            scale.y = -Mathf.Abs(scale.y);
        transform.localScale = scale;
    }
}