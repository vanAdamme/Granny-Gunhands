using UnityEngine;
using UnityEngine.Serialization;

public sealed class AudioController : MonoBehaviour, IAudioService
{
    // Expose the singleton as the interface (DIP-friendly)
    public static IAudioService Instance { get; private set; }

    [Header("UI")]
    [FormerlySerializedAs("pause")]         [SerializeField] private AudioSource pauseSource;
    [FormerlySerializedAs("unpause")]       [SerializeField] private AudioSource unpauseSource;
    [FormerlySerializedAs("selectUpgrade")] [SerializeField] private AudioSource selectUpgradeSource;
    [FormerlySerializedAs("gameOver")]      [SerializeField] private AudioSource gameOverSource;

    // Interface properties (and public API) – same names your callers use
    public AudioSource pause         => pauseSource;
    public AudioSource unpause       => unpauseSource;
    public AudioSource selectUpgrade => selectUpgradeSource;
    public AudioSource gameOver      => gameOverSource;

    private void Awake()
    {
        if (Instance != null && !ReferenceEquals(Instance, this))
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // === IAudioService ===
    public void PlaySound(AudioSource source)
    {
        if (!IsPlayable(source)) return;
        source.pitch = 1f;
        source.time = 0f;
        source.Stop();
        source.Play();
    }

    public void PlayModifiedSound(AudioSource source, float minPitch = 0.9f, float maxPitch = 1.1f)
    {
        if (!IsPlayable(source)) return;
        source.pitch = Random.Range(minPitch, maxPitch);
        source.time = 0f;
        source.Stop();
        source.Play();
    }

    // === Helpers ===
    private static bool IsPlayable(AudioSource s)
        => s != null && s.enabled && s.gameObject.activeInHierarchy;
}