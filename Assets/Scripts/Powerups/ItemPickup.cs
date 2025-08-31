using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ItemPickup : MonoBehaviour
{
    public InventoryItemDefinition Definition { get; private set; }

    public void SetDefinition(InventoryItemDefinition def) => Definition = def;

    private void OnTriggerEnter2D(Collider2D other)
    {
        var inv = other.GetComponentInParent<ItemInventory>();
        if (!inv) return;

        if (Definition != null)
        {
            inv.Add(Definition, 1);
            Destroy(gameObject);
        }
    }
}