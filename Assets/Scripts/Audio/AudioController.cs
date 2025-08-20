using UnityEngine;
using UnityEngine.Serialization;
using System.Collections;

public sealed class AudioController : MonoBehaviour, IAudioService
{
    // Expose the singleton as the interface (DIP-friendly)
    public static IAudioService Instance { get; private set; }

    [Header("UI")]
    [FormerlySerializedAs("pause")]         [SerializeField] private AudioSource pauseSource;
    [FormerlySerializedAs("unpause")]       [SerializeField] private AudioSource unpauseSource;
    [FormerlySerializedAs("selectUpgrade")] [SerializeField] private AudioSource selectUpgradeSource;
    [FormerlySerializedAs("gameOver")]      [SerializeField] private AudioSource gameOverSource;

    // Interface properties (same names your callers use)
    public AudioSource pause         => pauseSource;
    public AudioSource unpause       => unpauseSource;
    public AudioSource selectUpgrade => selectUpgradeSource;
    public AudioSource gameOver      => gameOverSource;

    private void Awake()
    {
        // Ensure single instance
        if (Instance is AudioController existing && existing != this)
        {
            Destroy(gameObject);
            return;
        }

        // IMPORTANT: DontDestroyOnLoad only works for root objects.
        // If we're parented, detach to become root before calling DDoL.
        if (transform.parent != null && transform.root != transform)
        {
            transform.SetParent(null, true); // keep world position, become root
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

        float original = source.pitch;
        float newPitch = Random.Range(minPitch, maxPitch);

        source.pitch = newPitch;
        source.time = 0f;
        source.Stop();
        source.Play();

        if (source.clip)
            StartCoroutine(RestorePitchAfter(source, source.clip.length / Mathf.Max(0.01f, newPitch), original));
        else
            StartCoroutine(RestorePitchAfter(source, 0.1f, original));
    }

    // === Helpers ===
    private static bool IsPlayable(AudioSource s)
        => s != null && s.enabled && s.gameObject.activeInHierarchy;

    private IEnumerator RestorePitchAfter(AudioSource source, float delay, float targetPitch)
    {
        float t = 0f;
        while (t < delay)
        {
            t += Time.unscaledDeltaTime; // restore works even when paused
            yield return null;
        }
        if (source) source.pitch = targetPitch;
    }
}