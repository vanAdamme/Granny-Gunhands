using UnityEngine;

public class CookieBribeSpecial : SpecialWeaponBase
{
    [SerializeField] private GameObject decoyPrefab;
    [SerializeField] private float duration = 6f;

    protected override void ActivateInternal()
    {
        if (!decoyPrefab) return;
        var decoy = Instantiate(decoyPrefab, transform.position, Quaternion.identity);
        Destroy(decoy, duration);
    }
}