using UnityEngine;

public class WeaponUpgradePickup : PickupBase
{
    [SerializeField] private WeaponUpgradeItemDefinition upgradeItem;

    protected override Sprite GetIcon() => upgradeItem ? upgradeItem.Icon : null;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (consumed || !upgradeItem) return;

        var inv = other.GetComponentInParent<ItemInventory>();
        if (!inv) return;

        inv.Add(upgradeItem, 1);

        // This is the "picked up" toast. The "applied" toast still comes from UpgradeToastListener via UpgradeEvents.
        ShowToastTemplate(upgradeItem.DisplayName ?? "Weapon upgrade", ("name", upgradeItem.DisplayName ?? "Upgrade"));
        StartCoroutine(Consume());
    }
}