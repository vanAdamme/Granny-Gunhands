using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PowerUpPickup : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    public PowerUpDefinition Definition { get; private set; }

    public void SetDefinition(PowerUpDefinition def)
    {
        Definition = def;
        if (spriteRenderer && def.Icon) spriteRenderer.sprite = def.Icon;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var ctx = other.GetComponentInParent<IPlayerContext>();
        if (!ctx) return;

        if (Definition != null)
        {
            Definition.Apply(ctx); // or raise event / add to inventory
            Destroy(gameObject);
        }
    }
}