using UnityEngine;

public class CookieBribeSpecial : SpecialWeaponBase
{
    [SerializeField] private GameObject decoyPrefab;
    [SerializeField] private float duration = 6f;

    protected override void ActivateInternal()
    {
        if (!decoyPrefab) return;

        var pos = transform.position;
        var decoy = Instantiate(decoyPrefab, pos, Quaternion.identity);
        Destroy(decoy, duration);

        // SFX/VFX hooks can go here if you have them.
    }
}