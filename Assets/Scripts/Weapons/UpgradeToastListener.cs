// UpgradeToastListener.cs
using UnityEngine;

public class UpgradeToastListener : MonoBehaviour
{
    [SerializeField] private float seconds = 1.2f;

    private void OnEnable()
    {
        WeaponUpgradePickup.OnWeaponUpgraded += Handle;
    }

    private void OnDisable()
    {
        WeaponUpgradePickup.OnWeaponUpgraded -= Handle;
    }

    private void Handle(string weaponName, int newLevel, Sprite icon)
    {
        // TODO: Replace this with your actual UI toast.
        // Example: UIController.Instance?.ShowUpgradeToast(weaponName, newLevel, icon);
        Debug.Log($"UPGRADED: {weaponName} → L{newLevel}");
    }
}