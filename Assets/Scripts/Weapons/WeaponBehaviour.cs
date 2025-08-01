using UnityEngine;

public abstract class WeaponBehaviour : MonoBehaviour
{
    [SerializeField] protected SpriteRenderer spriteRenderer;
    public abstract void Fire();

    protected virtual void Awake()
    {
        spriteRenderer ??= GetComponentInChildren<SpriteRenderer>();
    }

    protected virtual void Update()
    {
        FlipSprite();
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