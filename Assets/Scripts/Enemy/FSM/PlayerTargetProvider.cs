using UnityEngine;

/// Provides the current player Transform without scattering scene searches.
/// Prefers an IPlayerProvider (e.g., GameSystems), with an explicit override for special cases.
public class PlayerTargetProvider : MonoBehaviour, ITargetProvider
{
    [Header("Player source (leave empty to auto-bind GameSystems)")]
    [SerializeField] private MonoBehaviour playerProviderSource; // must implement IPlayerProvider

    [Header("Optional explicit override (scene object only)")]
    [SerializeField] private Transform explicitPlayer;

    private IPlayerProvider provider;
    private Transform player;

    void Awake()
    {
        BindProvider();
    }

    void OnEnable()
    {
        Subscribe();
        Resolve(force: true);
    }

    void OnDisable()
    {
        Unsubscribe();
    }

    public bool TryGetTarget(out Transform t)
    {
        // Explicit override wins if valid
        if (IsValidSceneObject(explicitPlayer))
        {
            t = explicitPlayer;
            return true;
        }

        if (IsValidSceneObject(player))
        {
            t = player;
            return true;
        }

        // Last-ditch: resolve once (in case provider appeared later)
        Resolve(force: false);
        t = IsValidSceneObject(player) ? player : null;
        return t != null;
    }

    // ---------- internals ----------

    private void BindProvider()
    {
        // 1) Serialized provider if it implements IPlayerProvider
        provider = playerProviderSource as IPlayerProvider;

        // 2) GameSystems singleton
        if (provider == null && GameSystems.Instance != null)
            provider = GameSystems.Instance;

        // 3) Fallback: find GameSystems (includes inactive)
        if (provider == null)
        {
            var gs = Object.FindFirstObjectByType<GameSystems>(FindObjectsInactive.Include);
            if (gs) provider = gs;
        }
    }

    private void Subscribe()
    {
        if (provider != null)
            provider.PlayerChanged += OnPlayerChanged;
    }

    private void Unsubscribe()
    {
        if (provider != null)
            provider.PlayerChanged -= OnPlayerChanged;
    }

    private void OnPlayerChanged(PlayerController p)
    {
        player = p ? p.transform : null;
    }

    private void Resolve(bool force)
    {
        if (IsValidSceneObject(explicitPlayer))
        {
            player = explicitPlayer;
            return;
        }

        if (provider == null) BindProvider();

        var cur = provider?.Player;
        if (cur)
        {
            player = cur.transform;
            return;
        }

        // Absolute fallback: single scene search (Unity 6-safe)
        var pc = Object.FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);
        player = pc ? pc.transform : null;
    }

    private static bool IsValidSceneObject(Transform t)
        => t && t.gameObject.scene.IsValid();
}