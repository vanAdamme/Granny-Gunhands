using UnityEngine;

public class CookieBribeSpecial : SpecialWeaponBase
{
    [Header("Cookie Bribe")]
    [SerializeField] private CookieBribeDecoy decoyPrefab;
    [SerializeField] private float maxPlaceRange = 8f;
    [SerializeField] private GameObject castVFX;

    Camera cam;

    protected override void Awake()
    {
        base.Awake();
        cam = Camera.main;
    }

    protected override bool ActivateSpecial()
    {
        if (!decoyPrefab) return false;
        var player = PlayerController.Instance;
        if (!player) return false;

        // Place at mouse within range of the player
        Vector3 mouse = cam ? cam.ScreenToWorldPoint(Input.mousePosition) : player.transform.position;
        mouse.z = 0f;

        Vector2 from = player.transform.position;
        Vector2 to = mouse;
        var dir = (to - from);
        float dist = dir.magnitude;
        if (dist > maxPlaceRange) to = from + dir.normalized * maxPlaceRange;

        Instantiate(decoyPrefab, to, Quaternion.identity);
        if (castVFX) VFX.Spawn(castVFX, player.transform.position, Quaternion.identity, 1f);

        return true;
    }
}