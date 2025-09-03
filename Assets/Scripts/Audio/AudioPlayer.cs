using UnityEngine;
using System;

[RequireComponent(typeof(AudioSource))]
public sealed class AudioPlayer : MonoBehaviour
{
    private AudioSource _src;
    private Action<AudioPlayer> _onFinished;
    private bool _attached;

    public AudioSource Source => _src;

    void Awake() { _src = GetComponent<AudioSource>(); }

    public void Play(SoundEvent evt, AudioClip clip, Vector3? worldPos, Transform attachTo,
                     Action<AudioPlayer> onFinished)
    {
        _onFinished = onFinished;
        _attached = attachTo != null;

        if (_attached)
        {
            transform.SetParent(attachTo, worldPositionStays: false);
            transform.localPosition = Vector3.zero;
        }
        else
        {
            transform.SetParent(null, worldPositionStays: true);
            transform.position = worldPos ?? Vector3.zero;
        }

        _src.clip = clip;
        _src.loop = evt.loop;
        _src.volume = evt.volume;
        _src.pitch  = evt.pitch + UnityEngine.Random.Range(-evt.randomPitchRange, evt.randomPitchRange);
        _src.spatialBlend = evt.spatialBlend;
        _src.outputAudioMixerGroup = evt.mixerGroup;
        _src.Play();

        if (!evt.loop) Invoke(nameof(CheckFinish), clip.length + 0.05f);
    }

    public void StopImmediate()
    {
        _src.Stop();
        Cleanup();
    }

    void Update()
    {
        if (!_src.loop && !_src.isPlaying && _src.clip != null)
            Cleanup();
        else if (_attached && transform.parent == null) // lost parent, ensure we don't leak
            Cleanup();
    }

    private void CheckFinish()
    {
        if (!_src.loop) Cleanup();
    }

    private void Cleanup()
    {
        CancelInvoke();
        _src.clip = null;
        transform.SetParent(null);
        _onFinished?.Invoke(this);
        _onFinished = null;
    }
}