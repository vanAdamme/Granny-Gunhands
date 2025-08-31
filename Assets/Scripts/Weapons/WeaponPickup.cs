using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class WeaponPickup : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private string sortingLayerName = "Items";
    [SerializeField] private int sortingOrder = 0;

    public WeaponDefinition Definition { get; private set; }

    public void SetDefinition(WeaponDefinition def)
    {
        Definition = def;
        // Optionally set sprite/icon from def here
        if (spriteRenderer)
        {
            spriteRenderer.sortingLayerName = sortingLayerName;
            spriteRenderer.sortingOrder = sortingOrder;
            if (def.Icon != null) spriteRenderer.sprite = def.Icon;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var inv = other.GetComponentInParent<WeaponInventory>();
        if (!inv) return;

        if (Definition != null && inv.TryAdd(Definition)) // your inventory API
        {
            Destroy(gameObject);
        }
    }
}