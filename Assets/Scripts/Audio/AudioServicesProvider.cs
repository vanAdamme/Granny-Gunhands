using UnityEngine;

public sealed class AudioServicesProvider : MonoBehaviour
{
    [SerializeField] private MonoBehaviour audioService; // assign AudioService
    [SerializeField] private MonoBehaviour musicService; // assign MusicService

    public static IAudioService Audio { get; private set; }
    public static IMusicService Music { get; private set; }

    void Awake()
    {
        if (transform.parent != null && transform.root != transform) transform.SetParent(null, true);
        DontDestroyOnLoad(gameObject);

        Audio ??= audioService as IAudioService ?? FindFirstObjectByType<AudioService>();
        Music ??= musicService as IMusicService ?? FindFirstObjectByType<MusicService>();
    }
}