using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class WeaponPickup : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private string sortingLayerName = "Items";
    [SerializeField] private int sortingOrder = 0;

    // Keep a private field so both Init() and SetDefinition() are trivial.
    [SerializeField] private WeaponDefinition definition;
    [SerializeField] private bool autoEquipToEmptyHand = true;

    void Awake()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
        tag = "Item";

        if (spriteRenderer)
        {
            spriteRenderer.sortingLayerName = sortingLayerName;
            spriteRenderer.sortingOrder = sortingOrder;
        }
    }

    // Called by LootTableDefinition variant A
    public void Init(WeaponDefinition def)
    {
        definition = def;
        if (spriteRenderer)
        {
            spriteRenderer.sortingLayerName = sortingLayerName;
            spriteRenderer.sortingOrder = sortingOrder;
            if (def && def.Icon) spriteRenderer.sprite = def.Icon;
        }
    }

    // Called by LootTableDefinition variant B
    public void SetDefinition(WeaponDefinition def)
    {
        definition = def;
        if (spriteRenderer && def && def.Icon) spriteRenderer.sprite = def.Icon;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Use InParent for safety if the collider is on a child.
        var inv = other.GetComponentInParent<WeaponInventory>();
        if (!inv || !definition) return;

        var added = inv.AddWeapon(definition, autoEquipToEmptyHand);
        if (added) Destroy(gameObject);
    }
}