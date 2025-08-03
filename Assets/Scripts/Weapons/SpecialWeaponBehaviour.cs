using UnityEngine;

public abstract class SpecialWeaponBehaviour : MonoBehaviour
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
        if (PlayerController.Instance == null) return;

        Vector3 scale = transform.localScale;

        if (transform.position.x > PlayerController.Instance.transform.position.x)
        {
            scale.y = Mathf.Abs(scale.y);
        }
        else
        {
            scale.y = -Mathf.Abs(scale.y);
        }

        transform.localScale = scale;
    }
}