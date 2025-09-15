using UnityEngine;

[DisallowMultipleComponent]
public class SpecialChargeFromDamage : MonoBehaviour
{
    [Header("Sources")]
    [Tooltip("If not assigned, will TryGetComponent<Health>() on this GameObject.")]
    [SerializeField] private Health health;

    [Header("Charge Target")]
    [Tooltip("Assign a MonoBehaviour that implements ISpecialCharge. " +
             "If not assigned, searches parent/children.")]
    [SerializeField] private MonoBehaviour chargeProvider; // must implement ISpecialCharge at runtime
    private ISpecialCharge charge;

    [Header("Tuning")]
    [Tooltip("Charge gained per 1.0 damage taken by this Health.")]
    [SerializeField, Min(0f)] private float chargePerDamage = 1f;

    [Tooltip("Guarantee at least this much charge on any positive damage (0 = disabled).")]
    [SerializeField, Min(0f)] private float minChargeOnHit = 0f;

    void Awake() => TryResolveRefs(editorLog: true);

    void OnEnable()
    {
        if (!health) TryResolveRefs(editorLog: false);
        if (health) health.Damaged += OnDamaged;  // ← requires the Health event shown below
    }

    void OnDisable()
    {
        if (health) health.Damaged -= OnDamaged;
    }

    private void OnDamaged(float amount, GameObject attacker)
    {
        if (charge == null || amount <= 0f || chargePerDamage <= 0f) return;

        float toAdd = amount * chargePerDamage;
        if (minChargeOnHit > 0f && toAdd < minChargeOnHit) toAdd = minChargeOnHit;

        if (toAdd > 0f)
            charge.AddDamage(toAdd);  // ← matches ISpecialCharge API
    }

    private void TryResolveRefs(bool editorLog)
    {
        if (!health && !TryGetComponent(out health))
        {
#if UNITY_EDITOR
            if (editorLog) Debug.LogWarning("[SpecialChargeFromDamage] No Health found on this GameObject.", this);
#endif
        }

        if (charge == null)
        {
            if (chargeProvider is ISpecialCharge c1) charge = c1;
            else
            {
                // Prefer parent (e.g., Managers HUD) then children
                charge = GetComponentInParent<ISpecialCharge>(includeInactive: true)
                         ?? GetComponentInChildren<ISpecialCharge>(includeInactive: true);
            }

#if UNITY_EDITOR
            if (editorLog && charge == null)
                Debug.LogWarning("[SpecialChargeFromDamage] No ISpecialCharge found. Assign a provider or add one to parent/children.", this);
#endif
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Re-resolve References")]
    private void EditorReresolve() => TryResolveRefs(editorLog: true);
#endif
}