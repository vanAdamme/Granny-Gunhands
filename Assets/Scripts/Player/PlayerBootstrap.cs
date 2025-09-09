using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class PlayerBootstrap : MonoBehaviour
{
    [SerializeField] private PlayerController playerPrefab;

    void Awake()
    {
        EnsurePlayer();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void EnsurePlayer()
    {
        // If a player already exists (domain reload off, or returning to play), don’t double-spawn.
        if (GameSystems.GetPlayer() != null) return;

        var p = Instantiate(playerPrefab);
        DontDestroyOnLoad(p.gameObject); // must be root object

        // PlayerController will self-register with GameSystems in its Awake/OnEnable.
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        var player = GameSystems.GetPlayer();
        if (!player) return;

        // Prefer an explicit spawn point in the scene
        var spawn = Object.FindFirstObjectByType<PlayerSpawnPoint>(FindObjectsInactive.Exclude);
        if (!spawn) return;

        var t = player.transform;
        t.position = spawn.transform.position;
        t.rotation = spawn.transform.rotation;

        // Reset movement
        if (player.TryGetComponent(out Rigidbody2D rb))
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        // Notify any components interested in spawn placement
        foreach (var s in player.GetComponentsInChildren<IPlayerSpawnable>(includeInactive: true))
            s.OnSpawnedAt(t.position, spawn.facing);
    }
}