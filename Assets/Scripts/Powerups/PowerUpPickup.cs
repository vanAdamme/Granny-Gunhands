using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PowerUpPickup : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;

    // NEW: let scene-placed pickups assign this in the Inspector
    [SerializeField] private PowerUpDefinition definition; 

    public void SetDefinition(PowerUpDefinition def)
    {
        definition = def;
        SyncVisual();
    }

    private void Awake()
    {
        // ensure triggers work
        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;

        // if dropped directly in scene, definition may already be set
        SyncVisual();
    }

#if UNITY_EDITOR
    private void OnValidate() => SyncVisual();
#endif

    private void SyncVisual()
    {
        if (spriteRenderer && definition && definition.Icon)
            spriteRenderer.sprite = definition.Icon;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!definition) return;

        // Use the controller so one-shots (like Heal) actually run
        var root = other.attachedRigidbody ? other.attachedRigidbody.transform.root : other.transform.root;
        var controller = root.GetComponentInChildren<PowerUpController>();
        if (controller)
        {
            controller.Apply(definition, vfxParentHint: root, pickupWorldOrigin: transform.position);
            Destroy(gameObject);
        }
    }
}