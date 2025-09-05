using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class WeaponUpgradePickup : PickupBase
{
    [SerializeField] private WeaponUpgradeItemDefinition upgradeItem;

    public void SetDefinition(WeaponUpgradeItemDefinition def)
    {
        upgradeItem = def;
        SyncVisual();
    }

    protected override Sprite GetIcon() => upgradeItem ? upgradeItem.Icon : null;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (consumed || !upgradeItem) return;

        var root = other.attachedRigidbody ? other.attachedRigidbody.transform.root : other.transform.root;
        var inv  = root ? root.GetComponentInChildren<ItemInventory>() : null;
        if (!inv)
        {
#if UNITY_EDITOR
            Debug.LogWarning("[WeaponUpgradePickup] No ItemInventory found on player root/children.", this);
#endif
            return;
        }

        // ItemInventory.Add(...) typically returns void; consume after adding.
        inv.Add(upgradeItem, 1);

        var n = upgradeItem.DisplayName ?? "Upgrade";
        ShowToastTemplate(n, ("name", n));
        StartCoroutine(Consume());
    }
}