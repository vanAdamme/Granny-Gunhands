using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class WeaponUpgradePickup : MonoBehaviour
{
    public static event System.Action<Weapon, int> OnWeaponUpgraded;

    [SerializeField] private WeaponUpgradeItemDefinition upgradeItem;
    [SerializeField] private MonoBehaviour toastServiceSource; // IToastService provider
    private IToastService toast;

    private bool consumed;
    private Collider2D col2d;
    private SpriteRenderer sr; // optional visual to hide instantly

    private void Awake()
    {
        col2d = GetComponent<Collider2D>();
        if (col2d) col2d.isTrigger = true;

        sr = GetComponentInChildren<SpriteRenderer>();
        tag = "Item";
        toast = toastServiceSource as IToastService;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (consumed) return;

        // Only react to the player hierarchy
        var root = other.attachedRigidbody ? other.attachedRigidbody.transform.root : other.transform.root;
        var ctx  = root.GetComponentInChildren<MonoBehaviour>() as IPlayerContext;
        if (ctx?.ItemInventory == null) return;

        // Guard immediately to avoid duplicate adds this frame
        consumed = true;
        if (col2d) col2d.enabled = false;
        if (sr) sr.enabled = false; // hide sprite right away

        ctx.ItemInventory.Add(upgradeItem, 1);
        toast?.Show("Weapon upgrade!");

        // Destroy at end of frame to be extra safe vs. multiple contact callbacks
        StartCoroutine(DestroyNextFrame());
    }

    private System.Collections.IEnumerator DestroyNextFrame()
    {
        yield return null;
        Destroy(gameObject);
    }

    // Internal helper so other scripts can raise the event safely.
    internal static void RaiseUpgraded(Weapon w, int appliedLevels)
    {
        OnWeaponUpgraded?.Invoke(w, appliedLevels);
    }
}