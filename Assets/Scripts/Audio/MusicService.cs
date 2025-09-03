using UnityEngine;

public sealed class MusicService : MonoBehaviour, IMusicService
{
    public static IMusicService Instance { get; private set; }

    private AudioSource _a, _b;
    private AudioSource _front, _back;
    private float _fadeT, _fadeDur;
    private bool _fading;

    void Awake()
    {
        if (Instance is MusicService existing && existing != this) { Destroy(gameObject); return; }
        if (transform.parent != null && transform.root != transform) transform.SetParent(null, true);
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _a = gameObject.AddComponent<AudioSource>();
        _b = gameObject.AddComponent<AudioSource>();
        _a.playOnAwake = _b.playOnAwake = false;
        _a.loop = _b.loop = true;

        _front = _a;
        _back  = _b;
    }

    public void Play(MusicTrack track, float fadeSeconds = 1f, bool loop = true)
    {
        if (!track || !track.clip) return;

        // swap roles: back becomes current, front is for the new track
        var tmp = _front; _front = _back; _back = tmp;

        _front.clip = track.clip;
        _front.outputAudioMixerGroup = track.mixerGroup;
        _front.loop = loop;
        _front.volume = 0f;
        _front.Play();

        _back.loop = true; // keep playing until fade starts
        _fadeT = 0f;
        _fadeDur = Mathf.Max(0.001f, fadeSeconds);
        _fading = true;
    }

    public void Stop(float fadeSeconds = 0.5f)
    {
        if (!_back.isPlaying && !_front.isPlaying) return;
        _fadeT = 0f;
        _fadeDur = Mathf.Max(0.001f, fadeSeconds);
        _fading = true;
        // stopping without a new track: fade both to zero
        _front.clip = _front.clip; // no-op; just to be explicit
    }

    void Update()
    {
        if (!_fading) return;

        _fadeT += Time.unscaledDeltaTime / _fadeDur;
        float t = Mathf.Clamp01(_fadeT);

        if (_front.clip)
        {
            _front.volume = t * 1f;   // fade in
        }
        _back.volume = (1f - t) * 1f; // fade out

        if (_fadeT >= 1f)
        {
            _fading = false;
            _back.Stop();
            _back.volume = 0f;
            if (!_front.clip) _front.Stop();
        }
    }
}