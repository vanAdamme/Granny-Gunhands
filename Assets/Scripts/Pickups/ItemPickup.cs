using UnityEngine;

public class ItemPickup : PickupBase
{
    [SerializeField] private InventoryItemDefinition definition;
    [SerializeField] private int amount = 1;

    public void SetDefinition(InventoryItemDefinition def) { definition = def; SyncVisual(); }

    protected override Sprite GetIcon() => definition ? definition.Icon : null; // if your items expose Icon

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (consumed || !definition) return;

        var inv = other.GetComponentInParent<ItemInventory>();
        if (!inv) return;

        inv.Add(definition, amount);

        ShowToastTemplate(definition.DisplayName, ("name", definition.DisplayName), ("amount", amount));
        StartCoroutine(Consume());
    }
}