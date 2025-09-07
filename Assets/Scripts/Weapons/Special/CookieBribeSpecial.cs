using UnityEngine;

public class CookieBribeSpecial : SpecialWeaponBase
{
    [SerializeField] private CookieBribeDecoy decoyPrefab;
    // [SerializeField] private float decoyLifetime = 6f;

    protected override void ActivateInternal()
    {
        if (!decoyPrefab) { Debug.LogWarning("[CookieBribeSpecial] No decoy prefab assigned."); return; }

        var player = PlayerController.Instance
                   ?? FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);

        var pos = player ? player.transform.position : transform.position;
        var decoy = Instantiate(decoyPrefab, pos, Quaternion.identity);

        // optional: if you kept SetLifetime(), call it; otherwise set lifetime via prefab
        var setter = decoy as CookieBribeDecoy;
        // decoy.SetLifetime(decoyLifetime); // include if you added this method
    }
}