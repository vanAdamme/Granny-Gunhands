using UnityEngine;

public class AggroOnDamageBridge : MonoBehaviour
{
    [SerializeField] private AggroTargetProvider aggro;

    void Reset()
    {
        if (!aggro) aggro = GetComponent<AggroTargetProvider>();
    }

    // Unity SendMessage handler: expects ONE param (we pass object[] from Health)
    public void OnDamaged(object payload)
    {
        if (!TryParsePayload(payload, out float _, out GameObject attacker)) return;
        if (!attacker) return;

        if (!aggro) aggro = GetComponent<AggroTargetProvider>();
        if (aggro) aggro.AggroFrom(attacker.transform);
    }

    // Optional: direct-call convenience (won't be used by SendMessage, but handy elsewhere)
    public void OnDamaged(float amount, GameObject attacker)
        => OnDamaged(new object[] { amount, attacker });

    private static bool TryParsePayload(object payload, out float amount, out GameObject attacker)
    {
        amount = 0f;
        attacker = null;

        // Allow either object[] or ValueTuple passed by other systems
        if (payload is object[] arr && arr.Length >= 2)
        {
            if (arr[0] is float f) amount = f;
            else if (arr[0] is int i) amount = i; // just in case
            attacker = arr[1] as GameObject;
            return true;
        }

        if (payload is (float a, GameObject go))
        {
            amount = a; attacker = go; return true;
        }

        return false;
    }
}