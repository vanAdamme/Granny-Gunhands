using UnityEngine;
using System;

public class PlayerShooting : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private MonoBehaviour inputServiceSource; // implements IInputService
    [SerializeField] private WeaponInventory inventory;
    [SerializeField] private Camera aimCamera;                 // leave null on prefab; will auto-resolve

    private IInputService input;

    private bool fireLeftHeld;
    private bool fireRightHeld;

    // Cached delegates
    private Action onFLStart, onFLCancel, onFRStart, onFRCancel;

    void Awake()
    {
        input = inputServiceSource as IInputService
             ?? InputService.Instance as IInputService
             ?? FindFirstObjectByType<InputService>(FindObjectsInactive.Include);

        if (!inventory) inventory = GetComponentInParent<WeaponInventory>()
                           ?? FindFirstObjectByType<WeaponInventory>(FindObjectsInactive.Include);

        if (!aimCamera) aimCamera = Camera.main;

        onFLStart  = () => fireLeftHeld  = true;
        onFLCancel = () => fireLeftHeld  = false;
        onFRStart  = () => fireRightHeld = true;
        onFRCancel = () => fireRightHeld = false;
    }

    void LateUpdate()
    {
        // Keep camera healthy if Cinemachine/scene swaps happen
        if (!aimCamera || !aimCamera.isActiveAndEnabled)
        {
            var main = Camera.main;
            if (main) aimCamera = main;
        }

        if (fireLeftHeld)  TryFire(inventory?.Left);
        if (fireRightHeld) TryFire(inventory?.Right);
    }

    void OnEnable()
    {
        if (input == null) return;
        input.FireLeftStarted   += onFLStart;
        input.FireLeftCanceled  += onFLCancel;
        input.FireRightStarted  += onFRStart;
        input.FireRightCanceled += onFRCancel;
    }

    void OnDisable()
    {
        if (input == null) return;
        if (onFLStart  != null) input.FireLeftStarted   -= onFLStart;
        if (onFLCancel != null) input.FireLeftCanceled  -= onFLCancel;
        if (onFRStart  != null) input.FireRightStarted  -= onFRStart;
        if (onFRCancel != null) input.FireRightCanceled -= onFRCancel;
    }

    private void TryFire(Weapon w)
    {
        if (!w) return;

        // Prefer aiming from the weapon’s muzzle for correctness
        var origin = w.Muzzle ? w.Muzzle.position : w.transform.position;

        // 1) Ask the input service for a direction (if it’s correct, great)
        Vector2 dir = Vector2.zero;
        if (input != null)
            dir = input.GetAimDirection(origin, aimCamera);

        // 2) Bullet-proof fallback: compute properly via ray/plane so Z always matches
        if (dir.sqrMagnitude < 0.0001f)
            dir = GetMouseAimDirection(origin, aimCamera);

        if (dir.sqrMagnitude > 0.0001f)
            w.TryFire(dir);
    }

    /// <summary>
    /// Returns a normalized 2D direction from origin to the mouse, intersecting the camera ray
    /// with the plane at origin.z so we never "aim above" due to wrong Z.
    /// </summary>
    private static Vector2 GetMouseAimDirection(Vector3 origin, Camera cam)
    {
        if (!cam) return Vector2.right;

        // Orthographic path: ScreenToWorldPoint ignores Z for XY; just clamp Z to origin
        if (cam.orthographic)
        {
            var mp = Input.mousePosition;
            var world = cam.ScreenToWorldPoint(mp);
            world.z = origin.z;
            var v = (Vector2)(world - origin);
            return v.sqrMagnitude > 0.0001f ? v.normalized : Vector2.right;
        }

        // Perspective path: intersect a ray with the Z-plane at origin.z
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        var plane = new Plane(Vector3.forward, new Vector3(0f, 0f, origin.z));
        if (plane.Raycast(ray, out float t))
        {
            Vector3 hit = ray.GetPoint(t);
            var v = (Vector2)(hit - origin);
            return v.sqrMagnitude > 0.0001f ? v.normalized : Vector2.right;
        }

        return Vector2.right;
    }
}
