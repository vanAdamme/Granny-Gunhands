using UnityEngine;
using UnityEngine.Pool;
using System.Collections.Generic;

public sealed class AudioService : MonoBehaviour, IAudioService
{
    public static IAudioService Instance { get; private set; }

    [Header("Pool")]
    [SerializeField] private int defaultCapacity = 16;
    [SerializeField] private int maxSize = 128;

    private ObjectPool<AudioPlayer> _pool;
    private readonly Dictionary<SoundEvent, float> _cooldowns = new();
    private readonly Dictionary<SoundEvent, int> _activeCounts = new();

    void Awake()
    {
        // Singleton for convenience (still accessed via interface).
        if (Instance is AudioService existing && existing != this) { Destroy(gameObject); return; }
        if (transform.parent != null && transform.root != transform) transform.SetParent(null, true);
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _pool = new ObjectPool<AudioPlayer>(
            createFunc: () =>
            {
                var go = new GameObject("AudioPlayer");
                go.transform.SetParent(transform);
                var ap = go.AddComponent<AudioPlayer>();
                return ap;
            },
            actionOnGet: ap => { ap.gameObject.SetActive(true); },
            actionOnRelease: ap => { ap.gameObject.SetActive(false); ap.transform.SetParent(transform, false); },
            actionOnDestroy: ap => { if (ap) Destroy(ap.gameObject); },
            collectionCheck: false, defaultCapacity, maxSize
        );
    }

    void Update()
    {
        // advance cooldown timers
        if (_cooldowns.Count == 0) return;
        List<SoundEvent> toClear = null;
        foreach (var kv in _cooldowns)
            if (Time.unscaledTime >= kv.Value)
            {
                (toClear ??= new()).Add(kv.Key);
            }
        if (toClear != null)
            for (int i = 0; i < toClear.Count; i++)
                _cooldowns.Remove(toClear[i]);
    }

    public IPlayingHandle Play(SoundEvent evt, Vector3? worldPos = null, Transform attachTo = null)
    {
        if (!evt || evt.clips == null || evt.clips.Length == 0) return null;

        // cooldown
        if (evt.cooldownSeconds > 0f && _cooldowns.ContainsKey(evt)) return null;

        // max simultaneous
        if (evt.maxSimultaneous > 0 &&
            _activeCounts.TryGetValue(evt, out var count) &&
            count >= evt.maxSimultaneous) return null;

        var clip = evt.clips.Length == 1
            ? evt.clips[0]
            : evt.clips[Random.Range(0, evt.clips.Length)];

        var player = _pool.Get();

        // increase active count
        _activeCounts[evt] = (_activeCounts.TryGetValue(evt, out var c) ? c : 0) + 1;

        player.Play(evt, clip, worldPos, attachTo, OnPlayerFinished);

        // set cooldown expiry
        if (evt.cooldownSeconds > 0f)
            _cooldowns[evt] = Time.unscaledTime + evt.cooldownSeconds;

        // capture evt to decrement on release
        void OnPlayerFinished(AudioPlayer p)
        {
            if (_activeCounts.TryGetValue(evt, out var running))
            {
                running = Mathf.Max(0, running - 1);
                if (running == 0) _activeCounts.Remove(evt); else _activeCounts[evt] = running;
            }
            _pool.Release(p);
        }

        return new PlayingHandle(player);
    }

    public IPlayingHandle PlayUI(SoundEvent evt)
    {
        // Force 2D at call-time without mutating the asset: clone a temp instance?
        // Cheap trick: just pass no worldPos/attach, and ensure evt.spatialBlend = 0 in the asset for UI events.
        return Play(evt, null, null);
    }

    public void StopAll(SoundEvent evt)
    {
        // Simple approach: iterate children and stop matching clips.
        // (We keep it O(n). If you need O(1), register instances per evt in a HashSet.)
        var players = GetComponentsInChildren<AudioPlayer>(true);
        foreach (var p in players)
        {
            var src = p.Source;
            if (!src || !src.clip || evt.clips == null) continue;
            for (int i = 0; i < evt.clips.Length; i++)
            {
                if (src.clip == evt.clips[i]) { p.StopImmediate(); break; }
            }
        }
    }

    public void SetBusVolume(string exposedParam, float linear01)
    {
        // Wire this to your AudioMixer externally (we don’t reference one here by design).
        // Expose a small MixerFacade if you want direct control from here.
        // For now, this is a seam for your UI sliders to call into a dedicated Mixer script.
    }
}