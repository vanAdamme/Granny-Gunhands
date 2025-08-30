using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PowerUpPickup : MonoBehaviour
{
    [SerializeField] private PowerUpDefinition powerUp;
    public void Init(PowerUpDefinition def) => powerUp = def;

    [Header("Optional: exact VFX parent")]
    [Tooltip("If set and the asset uses CustomParentHint, the effect will be parented to this Transform.")]
    [SerializeField] private Transform vfxParentOverride;

    private bool consumed;

    private void Awake()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
        tag = "Item";
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (consumed) return;
        var root = other.attachedRigidbody ? other.attachedRigidbody.transform.root : other.transform.root;
        var controller = root.GetComponentInChildren<PowerUpController>();
        if (!controller || !powerUp) return;

        // 1) Fire one-shot + start timed effects (controller will spawn duration VFX)
        consumed = true;
        controller.Apply(powerUp, vfxParentOverride, transform.position);

        // 2) Play pickup VFX immediately (short‑lived)
        if (powerUp.PickupVFXPrefab)
        {
            Transform parent = null;
            Vector3 worldPos;

            switch (powerUp.PickupVFXAttach)
            {
                case VFXAttachMode.PlayerRoot:
                    parent = other.transform; worldPos = other.transform.position; break;

                case VFXAttachMode.PickupOrigin:
                    parent = null; worldPos = transform.position; break;

                case VFXAttachMode.NamedAnchorOnCollector:
                    var anchors = other.GetComponent<VFXAttachPoints>();
                    parent = anchors ? anchors.Get(powerUp.PickupAnchorName) : other.transform;
                    worldPos = other.transform.position; break;

                case VFXAttachMode.CustomParentHint:
                    parent = vfxParentOverride ? vfxParentOverride : other.transform;
                    worldPos = other.transform.position; break;

                default:
                    parent = other.transform; worldPos = other.transform.position; break;
            }

            // PickupVFXLifetime <= 0 → auto (anim/particles)
            // PickupVFXLifetime  > 0 → at least that long; longer clips still win.
            if (parent)
                VFX.SpawnAttached(powerUp.PickupVFXPrefab, parent, worldPos, powerUp.PickupVFXLifetime);
            else
                VFX.Spawn(powerUp.PickupVFXPrefab, worldPos, Quaternion.identity, powerUp.PickupVFXLifetime);
        }

        Destroy(gameObject);
    }

    public void SetDefinition(PowerUpDefinition def)
    {
        var f = typeof(PowerUpPickup).GetField("powerUp",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        f?.SetValue(this, def);
    }
}