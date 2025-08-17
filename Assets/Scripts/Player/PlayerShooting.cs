using UnityEngine;

[RequireComponent(typeof(WeaponInventory))]
public class PlayerShooting : MonoBehaviour
{
    [SerializeField] private WeaponInventory inventory;
    private Camera cam;

    void Awake()
    {
        inventory ??= GetComponent<WeaponInventory>();
        cam = Camera.main;
    }

    void Update()
    {
        if (!cam || inventory == null) return;

        Vector3 mouse = cam.ScreenToWorldPoint(Input.mousePosition);
        mouse.z = 0f;

        // Aim from each weapon’s muzzle if available (nicer feel),
        // otherwise from the player root.
        Vector2 AimDir(Weapon w)
        {
            var origin = (w && w.Muzzle) ? w.Muzzle.position : transform.position;
            return ((Vector2)mouse - (Vector2)origin).normalized;
        }

        var left  = inventory.Left;
        var right = inventory.Right;

        bool lmbDown    = Input.GetMouseButton(0);
        bool lmbPressed = Input.GetMouseButtonDown(0);
        bool rmbDown    = Input.GetMouseButton(1);
        bool rmbPressed = Input.GetMouseButtonDown(1);

        if (left)
        {
            bool wantFire = left.Mode == Weapon.FireMode.FullAuto ? lmbDown : lmbPressed;
            if (wantFire) left.TryFire(AimDir(left));
        }

        if (right)
        {
            bool wantFire = right.Mode == Weapon.FireMode.FullAuto ? rmbDown : rmbPressed;
            if (wantFire) right.TryFire(AimDir(right));
        }
    }
}