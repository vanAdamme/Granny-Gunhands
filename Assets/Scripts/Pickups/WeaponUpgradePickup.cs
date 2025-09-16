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

        // Be explicit: only react to the player
        var player = other.GetComponentInParent<PlayerController>();
        if (!player) return; // something else touched us (weapon, VFX, debris, etc.)

        var inv = player.ItemInventory; // single source of truth
        if (!inv)
        {
#if UNITY_EDITOR
            Debug.LogWarning("[WeaponUpgradePickup] Player has no ItemInventory reference.", this);
#endif
            return;
        }

        inv.Add(upgradeItem, 1);

        var n = string.IsNullOrEmpty(upgradeItem.DisplayName) ? "Upgrade" : upgradeItem.DisplayName;
        ShowToastTemplate(n, ("name", n));
        StartCoroutine(Consume());
    }
}