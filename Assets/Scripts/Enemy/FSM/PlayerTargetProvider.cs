using UnityEngine;

public class PlayerTargetProvider : MonoBehaviour, ITargetProvider
{
    [Header("Optional explicit override (leave empty to auto-resolve)")]
    [SerializeField] private Transform explicitPlayer;

    [Header("Auto-resolve")]
    [SerializeField] private bool keepResolving = true;
    [SerializeField, Min(0.1f)] private float resolveEvery = 0.5f;

    private Transform player;
    private float nextResolveAt;

    void OnEnable()
    {
        Resolve(true);
    }

    void Update()
    {
        if (!keepResolving) return;

        // If player ref is missing, prefab-ish, or got destroyed, try again on an interval
        if (!IsValidSceneObject(player) && Time.unscaledTime >= nextResolveAt)
        {
            Resolve(false);
            nextResolveAt = Time.unscaledTime + resolveEvery;
        }
    }

    public bool TryGetTarget(out Transform t)
    {
        if (!IsValidSceneObject(player))
            Resolve(false);

        t = player;
        return t != null;
    }

    private void Resolve(bool force)
    {
        Transform found = null;

        // 1) Explicit override wins (if it's a scene object)
        if (IsValidSceneObject(explicitPlayer)) found = explicitPlayer;

        // 2) Singleton instance if available
        if (!found && PlayerController.Instance)
            found = PlayerController.Instance.transform;

        // 3) Fallback search (includes disabled/inactive)
        if (!found)
        {
            var p = Object.FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);
            if (p) found = p.transform;
        }

        if (found && found != player)
            player = found;
    }

    private static bool IsValidSceneObject(Transform t)
        => t && t.gameObject.scene.IsValid();
}