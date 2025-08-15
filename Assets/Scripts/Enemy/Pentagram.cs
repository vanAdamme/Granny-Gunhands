using UnityEngine;

public class Pentagram : MonoBehaviour
{
    [SerializeField] private DamageFlash damageFlash;
    [SerializeField] LayerMask triggerLayer;

    static bool InMask(int layer, LayerMask mask) => (mask.value & (1 << layer)) != 0;

    void OnTriggerStay2D(Collider2D other)
    {
        if (!damageFlash) return;

        if (InMask(other.gameObject.layer, triggerLayer))
            damageFlash.CallDamageFlash();
    }
}