using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-10000)]
[DisallowMultipleComponent]
public sealed class AudioBootstrapper : MonoBehaviour
{
    [Header("Prefab with AudioService, MusicService, and AudioServicesProvider")]
    [SerializeField] private GameObject audioRootPrefab;

    [SerializeField] private bool logInfo = true;

    private static bool s_bootstrapped;

    void Awake()
    {
        // If we already ran once in this play session, just trim any duplicates and bail.
        if (s_bootstrapped)
        {
            TryTrimDuplicates("already bootstrapped");
            Destroy(gameObject);
            return;
        }

        // Do we already have services in the project (including inactive objects)?
        bool haveAudio = FindFirstObjectByType<AudioService>(FindObjectsInactive.Include) != null;
        bool haveMusic = FindFirstObjectByType<MusicService>(FindObjectsInactive.Include) != null;

        if (!haveAudio || !haveMusic)
        {
            if (!audioRootPrefab)
            {
                Debug.LogError("[AudioBootstrapper] No AudioRoot prefab assigned and no services found.");
            }
            else
            {
                var go = Instantiate(audioRootPrefab);
                go.name = go.name.Replace("(Clone)", "") + " (Runtime)";
                if (go.transform.parent != null) go.transform.SetParent(null, true);
                DontDestroyOnLoad(go);
                if (logInfo) Debug.Log("[AudioBootstrapper] Spawned AudioRoot.");
            }
        }
        else if (logInfo)
        {
            Debug.Log("[AudioBootstrapper] Audio services already present; not spawning.");
        }

        s_bootstrapped = true;
        TryTrimDuplicates("after bootstrap");

        SceneManager.sceneLoaded += OnSceneLoaded;
        DontDestroyOnLoad(gameObject);
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryTrimDuplicates($"scene loaded: {scene.name}");
    }

    private static void TryTrimDuplicates(string context)
    {
        TrimDuplicates<AudioService>(context);
        TrimDuplicates<MusicService>(context);
        TrimDuplicates<AudioServicesProvider>(context);
    }

    private static void TrimDuplicates<T>(string context) where T : MonoBehaviour
    {
        var all = FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (all == null || all.Length <= 1) return;

        // Prefer the one living in the DontDestroyOnLoad scene
        T keep = all[0];
        foreach (var c in all)
        {
            if (!c) continue;
            if (c.gameObject.scene.buildIndex == -1) { keep = c; break; }
        }

        foreach (var c in all)
        {
            if (!c || ReferenceEquals(c, keep)) continue;
            Debug.LogWarning($"[AudioBootstrapper] Duplicate {typeof(T).Name} found ({context}) on '{c.gameObject.name}'. Destroying duplicate.");
            Destroy(c.gameObject);
        }
    }

    // Safety: if Fast Enter Play Mode is enabled (no domain reload), reset our static flag at app start
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        s_bootstrapped = false;
    }
}