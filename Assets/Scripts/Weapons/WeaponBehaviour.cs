using UnityEngine;

public abstract class WeaponBehaviour : MonoBehaviour
{
    [SerializeField] protected SpriteRenderer spriteRenderer;
    private SpriteRenderer bodyRenderer;

    public abstract void Fire();

    protected virtual void Awake()
    {
        spriteRenderer ??= GetComponentInChildren<SpriteRenderer>();

         // Automatically find the parent body’s SpriteRenderer
        bodyRenderer = GetComponentInParent<SpriteRenderer>();
        if (bodyRenderer == null)
        {
            Debug.LogWarning($"{name}: No parent SpriteRenderer found for WeaponBehaviour.");
        }
    }

    protected virtual void Update()
    {
        FlipSprite();
    }

    protected virtual void LateUpdate()
    {
        // Ensure weapon is drawn above the body
        if (spriteRenderer != null && bodyRenderer != null)
        {
            spriteRenderer.sortingLayerID = bodyRenderer.sortingLayerID;
            spriteRenderer.sortingOrder = bodyRenderer.sortingOrder + 1;
        }
    }

    protected void FlipSprite()
    {
        // correct orientation
        if (transform.position.x > PlayerController.Instance.transform.position.x)
        {
            spriteRenderer.flipY = false;
        }
        else
        {
            spriteRenderer.flipY = true;
        }
    }
}