using UnityEngine;

public class MountAimer : MonoBehaviour
{
    [SerializeField] private MonoBehaviour inputServiceSource; // IInputService provider
    private IInputService input;

    [Tooltip("Optional: the transform to rotate. Defaults to this transform.")]
    [SerializeField] private Transform pivot;

    [Tooltip("Leave empty on prefabs. Will auto-resolve Camera.main at runtime.")]
    [SerializeField] private Camera worldCamera;

    [Header("Behaviour")]
    [SerializeField] private bool autoResolveCamera = true; // keep trying if camera is missing/changes
    [SerializeField] private bool warnIfNoCamera = true;

    void Awake()
    {
        input = inputServiceSource as IInputService;
        if (input == null)
            input = FindFirstObjectByType<InputService>(FindObjectsInactive.Include);

        if (!pivot) pivot = transform;

        // First attempt – okay if this is null in a prefab, we’ll keep resolving later.
        if (!worldCamera) worldCamera = Camera.main;
    }

    void LateUpdate()
    {
        if (autoResolveCamera && (worldCamera == null || !worldCamera.isActiveAndEnabled))
            TryResolveCamera();

        if (input == null) return; // no input, nothing to aim with
        if (!pivot) return;

        var dir = input.GetAimDirection(pivot.position, worldCamera);
        if (dir.sqrMagnitude > 0.0001f)
            pivot.right = dir;
    }

    void TryResolveCamera()
    {
        // Prefer the tagged main camera – works with Cinemachine Brain on the main camera.
        var cam = Camera.main;

        // Fallback: find any enabled camera in the scene (handles odd setups)
        if (!cam)
            cam = FindFirstObjectByType<Camera>(FindObjectsInactive.Exclude);

        if (cam)
        {
            worldCamera = cam;
        }
        else if (warnIfNoCamera)
        {
            // Only warn occasionally to avoid log spam
            if (Time.frameCount % 60 == 0)
                Debug.LogWarning("[MountAimer] No active Camera found. Tag your gameplay camera as MainCamera.");
        }
    }
}