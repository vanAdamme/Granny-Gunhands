using UnityEngine;

public class PowerUpPickup : PickupBase
{
    [SerializeField] private PowerUpDefinition definition;

    public void SetDefinition(PowerUpDefinition def) { definition = def; SyncVisual(); }

    protected override Sprite GetIcon() => definition ? definition.Icon : null;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (consumed || !definition) return;

        var controller = other.GetComponentInParent<PowerUpController>();
        if (!controller) return;

        bool didAnything = controller.Apply(definition, vfxParentHint: null, pickupWorldOrigin: transform.position);
        if (didAnything)
        {
            var n = definition.DisplayName;
            ShowToastTemplate(n, ("name", n));     // uses the Inspector template if set, else falls back to n
            StartCoroutine(Consume());
        }
        else
        {
            ShowToast("HP is already full");       // explain why it didn’t disappear
            // do NOT consume
        }
    }
}