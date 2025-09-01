// WeaponPickup.cs
using UnityEngine;

public class WeaponPickup : PickupBase
{
    [SerializeField] private WeaponDefinition definition;
    [SerializeField] private bool autoEquipToEmptyHand = true;

    public void SetDefinition(WeaponDefinition def) { definition = def; SyncVisual(); }
    public void Init(WeaponDefinition def)          => SetDefinition(def);

    protected override Sprite GetIcon() => definition ? definition.Icon : null;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (consumed || !definition) return;

        var inv = other.GetComponentInParent<WeaponInventory>();
        if (!inv) return;

        if (inv.AddWeapon(definition, autoEquipToEmptyHand))
        {
            ShowToastTemplate(definition.DisplayName, ("name", definition.DisplayName));
            StartCoroutine(Consume());
        }
    }
}