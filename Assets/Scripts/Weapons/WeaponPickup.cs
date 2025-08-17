using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class WeaponPickup : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private string sortingLayerName = "Items";
    [SerializeField] private int sortingOrder = 0;

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

    void OnTriggerEnter2D(Collider2D other)
    {
        var inv = other.GetComponent<WeaponInventory>();
        if (!inv || !definition) return;

        var added = inv.AddWeapon(definition, autoEquipToEmptyHand);
        if (added) Destroy(gameObject);
    }

    public void SetDefinition(WeaponDefinition def)
    {
        var f = typeof(WeaponPickup).GetField("definition",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        f?.SetValue(this, def);
    }
}