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

    void EnsurePlayer()
    {
        // If you already have one (e.g., reenter play), don’t spawn another
        if (PlayerController.Instance != null) return;

        var p = Instantiate(playerPrefab);
        DontDestroyOnLoad(p.gameObject);       // must be root object
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Find a spawn in the new scene
        var spawn = FindFirstObjectByType<PlayerSpawnPoint>(FindObjectsInactive.Exclude);
        if (spawn && PlayerController.Instance)
        {
            var t = PlayerController.Instance.transform;
            t.position = spawn.transform.position;
            t.rotation = spawn.transform.rotation;

            var rb = PlayerController.Instance.GetComponent<Rigidbody2D>();
            if (rb) { rb.linearVelocity = Vector2.zero; rb.angularVelocity = 0f; }
        }
    }
}