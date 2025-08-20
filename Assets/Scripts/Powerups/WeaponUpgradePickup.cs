using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class WeaponUpgradePickup : MonoBehaviour
{
    public static event System.Action<Weapon, int> OnWeaponUpgraded;
    [SerializeField] private WeaponUpgradeItemDefinition upgradeItem;

    private void Awake()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
        tag = "Item";
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!upgradeItem) return;

        var root = other.attachedRigidbody ? other.attachedRigidbody.transform.root : other.transform.root;
        var bag  = root.GetComponentInChildren<ItemInventory>();
        if (!bag) return;

        bag.Add(upgradeItem, 1);
        Destroy(gameObject);
    }
    
    // Internal helper so other scripts can raise the event safely.
    internal static void RaiseUpgraded(Weapon w, int appliedLevels)
    {
        OnWeaponUpgraded?.Invoke(w, appliedLevels);
    }
}