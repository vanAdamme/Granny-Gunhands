using UnityEngine;

public class UpgradeToastListener : MonoBehaviour
{
    private UIController ui;

    private void Awake()
    {
        ui = UIController.Instance ?? FindFirstObjectByType<UIController>(); // Unity 6-safe
    }

    private void OnEnable()
    {
        WeaponUpgradePickup.OnWeaponUpgraded += Handle;
    }

    private void OnDisable()
    {
        WeaponUpgradePickup.OnWeaponUpgraded -= Handle;
    }

    private void Handle(Weapon weapon, int newLevel)
    {
        if (!ui) return;

        string name = weapon && weapon.Definition
            ? weapon.Definition.DisplayName
            : weapon ? weapon.name : "Weapon";

        var icon = weapon ? weapon.icon : null;

        ui.ShowUpgradeToast(name, newLevel, icon);
    }
}