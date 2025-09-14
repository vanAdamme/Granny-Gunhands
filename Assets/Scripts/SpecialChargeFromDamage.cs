using System;
using System.Reflection;
using UnityEngine;

public sealed class SpecialChargeFromDamage : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] private MonoBehaviour specialChargeSource; // must implement ISpecialCharge
    private ISpecialCharge charge;

    [Header("Tuning")]
    [SerializeField, Min(0f)] private float chargePerDamage = 1f;

    void Awake()
    {
        charge = specialChargeSource as ISpecialCharge;
        if (charge != null) return;

#if UNITY_6000_0_OR_NEWER
        var all = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        var all = FindObjectsOfType<MonoBehaviour>(includeInactive: true);
#endif
        foreach (var mb in all)
            if (mb is ISpecialCharge sc) { charge = sc; break; }

        if (charge == null)
            Debug.LogWarning("[SpecialChargeFromDamage] No ISpecialCharge found. Assign one in the Inspector.");
    }

    void OnEnable()  => DamageEvents.Damaged += OnDamaged;
    void OnDisable() => DamageEvents.Damaged -= OnDamaged;

    // Matches Action<GameObject victim, Component source, float amount>
    private void OnDamaged(GameObject victim, Component source, float amount)
    {
        if (charge == null) return;

        // Only grant when the PLAYER is the attacker
        if (source && source.gameObject == PlayerController.Instance?.gameObject)
        {
            int add = Mathf.Max(0, Mathf.RoundToInt(amount * chargePerDamage));
            TryAddCharge(add);
        }
    }

    // --- Helpers ---

    void TryAddCharge(int add)
    {
        if (add <= 0 || charge == null) return;

        var t = charge.GetType();

        // Try common methods first
        var m =
            t.GetMethod("Add",           BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(int) }, null) ??
            t.GetMethod("AddCharge",     BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(int) }, null) ??
            t.GetMethod("Gain",          BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(int) }, null) ??
            t.GetMethod("Increase",      BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(int) }, null) ??
            t.GetMethod("Increment",     BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(int) }, null);

        if (m != null) { m.Invoke(charge, new object[] { add }); return; }

        // Fallback: bump Current (clamped) if writeable or via SetCurrent(int)
        var pCur = t.GetProperty("Current");
        var pReq = t.GetProperty("Required");
        int cur = pCur != null && pCur.CanRead ? Convert.ToInt32(pCur.GetValue(charge)) : 0;
        int req = pReq != null && pReq.CanRead ? Convert.ToInt32(pReq.GetValue(charge)) : int.MaxValue;
        int newVal = Mathf.Clamp(cur + add, 0, req);

        if (pCur != null && pCur.CanWrite)          { pCur.SetValue(charge, newVal); return; }
        var setCurrent = t.GetMethod("SetCurrent");  if (setCurrent != null)         { setCurrent.Invoke(charge, new object[] { newVal }); return; }

        Debug.LogWarning("[SpecialChargeFromDamage] Could not add charge; ISpecialCharge implementation exposes no compatible API.");
    }
}