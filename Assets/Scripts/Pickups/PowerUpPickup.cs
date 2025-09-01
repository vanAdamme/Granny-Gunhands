using UnityEngine;

public class PowerUpPickup : PickupBase
{
    [SerializeField] private PowerUpDefinition definition;

    public void SetDefinition(PowerUpDefinition def) { definition = def; SyncVisual(); }

    protected override Sprite GetIcon() => definition ? definition.Icon : null;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (consumed || !definition) return;

        var root       = other.attachedRigidbody ? other.attachedRigidbody.transform.root : other.transform.root;
        var controller = root.GetComponentInChildren<PowerUpController>();
        if (!controller) return;

        controller.Apply(definition, vfxParentHint: root, pickupWorldOrigin: transform.position);

        // toast: falls back to DisplayName if no template is set
        var name = definition.DisplayName;
        ShowToastTemplate(name, ("name", name));

        StartCoroutine(Consume());
    }
}