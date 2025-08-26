using UnityEngine;

[CreateAssetMenu(menuName = "Items/Weapon Upgrade")]
public class WeaponUpgradeItemDefinition : InventoryItemDefinition
{
    [Header("Upgrade Rule")]
    public WeaponCategory category = WeaponCategory.Pistol;
    [Min(1)] public int levels = 1;
    [Tooltip("Only consider currently equipped weapons of this category.")]
    public bool equippedOnly = true;

    public override bool CanUse(GameObject user) => false;
    public override bool TryUse(GameObject user) => false;
}