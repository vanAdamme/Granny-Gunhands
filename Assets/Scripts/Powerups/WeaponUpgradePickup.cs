using UnityEngine;

public class WeaponUpgradePickup : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        var inv = other.GetComponent<WeaponInventory>();
        if (!inv) return;

        if (inv.Right != null && inv.Right.TryUpgrade())
        {
            Destroy(gameObject);
        }
    }
}