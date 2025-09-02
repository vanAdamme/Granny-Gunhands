using UnityEngine;

public class WeaponPickup : PickupBase
{
    [SerializeField] private WeaponDefinition definition;           // scene-placed or set by LootTable
    [SerializeField] private bool autoEquipToEmptyHand = true;

    public void SetDefinition(WeaponDefinition def) { definition = def; SyncVisual(); }
    public void Init(WeaponDefinition def)          => SetDefinition(def);

    protected override Sprite GetIcon() => definition ? definition.Icon : null;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (consumed || !definition) return;

        // Mirror the root/child handling used in PowerUpPickup
        var root = other.attachedRigidbody ? other.attachedRigidbody.transform.root : other.transform.root;
        var inv  = root.GetComponentInChildren<WeaponInventory>();
        if (!inv) return;

        var added = inv.AddWeapon(definition, autoEquipToEmptyHand);
        if (added != null)                   // AddWeapon returns the Weapon instance on success
        {
            ShowToast(definition.DisplayName);
            StartCoroutine(Consume());
        }
#if UNITY_EDITOR
        else
        {
            Debug.Log($"[WeaponPickup] '{definition.DisplayName}' not added (maybe already owned?).", this);
        }
#endif
    }
}