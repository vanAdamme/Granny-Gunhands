using UnityEngine;

public class Pentagram : MonoBehaviour
{
    [SerializeField] private DamageFlash damageFlash;

    void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player") && (damageFlash != null))
        {
            damageFlash.CallDamageFlash();
        }
    }
}