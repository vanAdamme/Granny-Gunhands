using UnityEngine;

public class WeaponUpgradeItem : MonoBehaviour
{
    // If this is a world pickup:
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Example: auto-upgrade right-hand weapon
        var inv = other.GetComponent<WeaponInventory>();
        if (!inv) return;

        var w = inv.Right as GenericProjectileWeapon;
        if (w && w.TryUpgrade())
        {
            // You could refresh UI icons here, if not already via events
            Destroy(gameObject);
        }
    }

    // If used via inventory drag-drop, call this:
    public bool TryApplyToWeapon(Weapon weapon)
    {
        var g = weapon as GenericProjectileWeapon;
        if (!g) return false;

        if (g.TryUpgrade())
        {
            // TODO: remove this item from inventory stack
            return true;
        }
        return false;
    }
}