using UnityEngine;

public class MountAimer : MonoBehaviour
{
    [SerializeField] private MonoBehaviour inputServiceSource; // InputService
    private IInputService input;

    [Tooltip("Optional: the transform to rotate. Defaults to this transform.")]
    [SerializeField] private Transform pivot;

    [SerializeField] private Camera worldCamera; // default main

    void Awake()
    {
        input = inputServiceSource as IInputService;
        if (input == null) input = FindFirstObjectByType<InputService>();
        if (!pivot) pivot = transform;
        if (!worldCamera) worldCamera = Camera.main;
    }

    void LateUpdate()
    {
        if (input == null) return;

        var dir = input.GetAimDirection(pivot.position, worldCamera);
        if (dir.sqrMagnitude > 0.0001f)
            pivot.right = dir; // align with Weapon.Fire() using muzzle.right
    }
}