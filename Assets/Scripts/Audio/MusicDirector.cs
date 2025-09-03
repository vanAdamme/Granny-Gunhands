using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-1000)]
public class MusicDirector : MonoBehaviour
{
    [SerializeField] private SceneMusicMap map;
    [SerializeField] private float fadeSeconds = 1f;

    void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        // play for the initial scene too
        PlayFor(SceneManager.GetActiveScene().name);
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode) => PlayFor(scene.name);

    private void PlayFor(string sceneName)
    {
        var track = map ? map.GetFor(sceneName) : null;
        if (track) AudioServicesProvider.Music?.Play(track, fadeSeconds, loop: track.loop);
    }
}