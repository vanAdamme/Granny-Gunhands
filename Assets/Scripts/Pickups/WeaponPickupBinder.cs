using UnityEngine;

[DisallowMultipleComponent]
public class WeaponPickupBinder : MonoBehaviour
{
    [SerializeField] private WeaponPickup pickup;        // auto-filled
    [SerializeField] private WeaponDefinition definition;

    void Awake() {
        if (!pickup) pickup = GetComponent<WeaponPickup>();
        if (pickup && definition) pickup.SetDefinition(definition);
    }

#if UNITY_EDITOR
    void OnValidate() {
        if (!pickup) pickup = GetComponent<WeaponPickup>();
        // optional: update sprite preview
        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr && definition && definition.Icon) sr.sprite = definition.Icon;
    }
#endif
}