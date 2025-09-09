using UnityEngine;

/// Attach this to the Player (or a central Systems/Managers object).
/// Listens for global damage events and feeds charge into an ISpecialCharge
/// when the PLAYER is the attacker (kills do NOT grant charge—only damage).
public sealed class SpecialChargeFromDamage : MonoBehaviour
{
    [Header("Wiring")]
    [Tooltip("Optional. Assign a component that implements ISpecialCharge. If left null, we'll scan the scene at startup.")]
    [SerializeField] private MonoBehaviour specialChargeSource; // must implement ISpecialCharge
    private ISpecialCharge charge;

    [Header("Tuning")]
    [Tooltip("How much charge to add per 1 damage dealt. 1 = 1:1 mapping; 0.5 halves it, 2 doubles it.")]
    [SerializeField, Min(0f)] private float chargePerDamage = 1f;

    void Awake()
    {
        // Prefer explicit DI via the Inspector
        charge = specialChargeSource as ISpecialCharge;
        if (charge != null) return;

        // Fallback: scan scene behaviours once and take the first implementing ISpecialCharge
#if UNITY_6000_0_OR_NEWER
        var all = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        var all = FindObjectsOfType<MonoBehaviour>(includeInactive: true);
#endif
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] is ISpecialCharge sc) { charge = sc; break; }
        }

        if (charge == null)
            Debug.LogWarning("[SpecialChargeFromDamage] No ISpecialCharge found. Assign one in the Inspector.");
    }

    void OnEnable()  => DamageEvents.Damaged += OnDamaged;
    void OnDisable() => DamageEvents.Damaged -= OnDamaged;

    private void OnDamaged(GameObject attacker, Component victim, float amount)
    {
        if (charge == null || amount <= 0f || chargePerDamage <= 0f) return;

        // Only award when the PLAYER dealt the damage (not on kills, not on incoming hits)
        var player = PlayerController.Instance ? PlayerController.Instance.gameObject : null;
        if (player == null || attacker != player) return;

        charge.AddDamage(amount * chargePerDamage);
    }
}