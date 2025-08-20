using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ItemPickup : MonoBehaviour
{
    [SerializeField] private InventoryItemDefinition item;
    [SerializeField] private int amount = 1;

    [Header("FX (optional)")]
    [SerializeField] private GameObject vfxOnPickup;
    [SerializeField] private float vfxLifetime = 1.5f;

    private void Awake()
    {
        var c = GetComponent<Collider2D>();
        c.isTrigger = true;
        // Tag this something like "Pickup" (NOT "Item") to avoid PlayerController auto-destroy.
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var inv = other.GetComponent<ItemInventory>();
        if (!inv) return;

        inv.Add(item, amount);

        if (vfxOnPickup)
        {
            var fx = Instantiate(vfxOnPickup, transform.position, Quaternion.identity);
            if (vfxLifetime > 0) Destroy(fx, vfxLifetime);
        }

        Destroy(gameObject);
    }
}