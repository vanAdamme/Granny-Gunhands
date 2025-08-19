using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider2D))]
public class WeaponUpgradePickup : MonoBehaviour
{
    [Header("Targeting")]
    [Tooltip("Allowed weapon categories. Empty = any category.")]
    [SerializeField] private List<WeaponCategory> allowedCategories = new List<WeaponCategory> { WeaponCategory.Pistol };

    [Tooltip("Only consider equipped (Left/Right) weapons.")]
    [SerializeField] private bool equippedOnly = true;

    [Tooltip("Destroy the pickup even if no matching/upgradeable weapon was found.")]
    [SerializeField] private bool consumeIfNoMatch = false;

    [Header("Feedback (optional)")]
    [SerializeField] private GameObject upgradeVFX;
    [SerializeField] private float vfxLifetime = 1.5f;

    // UI hook (wire any UI receiver in the Inspector; stays decoupled)
    [System.Serializable] public class UpgradeEvent : UnityEvent<string, int, Sprite> {}
    public UpgradeEvent onUpgraded;

    // Global C# event (optional, for code subscribers)
    public static event System.Action<string, int, Sprite> OnWeaponUpgraded;

    private void Awake()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
        tag = "Item"; // If you keep this, FIX PlayerController coin logic as below.
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var inv = other.GetComponent<WeaponInventory>();
        if (!inv) { if (consumeIfNoMatch) Destroy(gameObject); return; }

        // Build candidate list
        var candidates = new List<Weapon>(2);
        if (equippedOnly)
        {
            if (Matches(inv.Left))  candidates.Add(inv.Left);
            if (Matches(inv.Right)) candidates.Add(inv.Right);
        }
        else
        {
            foreach (var w in inv.GetInventory()) if (Matches(w)) candidates.Add(w);
        }

        if (candidates.Count == 0) { if (consumeIfNoMatch) Destroy(gameObject); return; }

        // Pick lowest level among candidates
        Weapon target = candidates[0];
        for (int i = 1; i < candidates.Count; i++)
            if (candidates[i].Level < target.Level) target = candidates[i];

        // Attempt upgrade
        if (target.TryUpgrade())
        {
            // VFX
            if (upgradeVFX)
            {
                var pos = target.Muzzle ? target.Muzzle.position : target.transform.position;
                var fx = Instantiate(upgradeVFX, pos, Quaternion.identity);
                if (vfxLifetime > 0f) Destroy(fx, vfxLifetime);
            }

            // SFX (safe; used elsewhere) 
            AudioController.Instance?.PlaySound(AudioController.Instance.selectUpgrade); // used in PlayerController too. :contentReference[oaicite:2]{index=2}

            // UI toast via hooks
            var name  = target.Definition ? target.Definition.DisplayName : target.name; // Definition exists. :contentReference[oaicite:3]{index=3}
            var icon  = target.icon;
            var level = target.Level;
            onUpgraded?.Invoke(name, level, icon);
            OnWeaponUpgraded?.Invoke(name, level, icon);

            Destroy(gameObject);
        }
        else
        {
            // Maxed: keep or consume based on flag
            if (consumeIfNoMatch) Destroy(gameObject);
        }
    }

    private bool Matches(Weapon w)
    {
        if (!w || !w.Definition) return false;                       // Definition used across systems. :contentReference[oaicite:4]{index=4}
        if (allowedCategories == null || allowedCategories.Count == 0) return true;
        return allowedCategories.Contains(w.Definition.Category);
    }
}