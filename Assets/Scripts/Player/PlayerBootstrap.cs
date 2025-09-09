using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class PlayerBootstrap : MonoBehaviour
{
    [SerializeField] private PlayerController playerPrefab;
    [SerializeField] private bool dontDestroyOnLoad = true;

    void Awake()
    {
        EnsurePlayer();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void EnsurePlayer()
    {
        if (GameSystems.GetPlayer() != null) return;

        var p = Instantiate(playerPrefab);
        if (dontDestroyOnLoad) DontDestroyOnLoad(p.gameObject); // must be root object
        // PlayerController registers itself with GameSystems in Awake/OnEnable.
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        var player = GameSystems.GetPlayer();
        if (!player) return;

        var spawn = Object.FindFirstObjectByType<PlayerSpawnPoint>(FindObjectsInactive.Exclude);
        if (!spawn) return;

        var t = player.transform;
        t.position = spawn.transform.position;
        t.rotation = spawn.transform.rotation;

        if (player.TryGetComponent(out Rigidbody2D rb))
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        foreach (var s in player.GetComponentsInChildren<IPlayerSpawnable>(includeInactive: true))
            s.OnSpawnedAt(t.position, spawn.facing);
    }
}