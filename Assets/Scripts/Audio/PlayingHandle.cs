using UnityEngine;

public sealed class PlayingHandle : IPlayingHandle
{
    private AudioPlayer _player;
    public PlayingHandle(AudioPlayer p) { _player = p; }

    public bool IsPlaying => _player && _player.Source && _player.Source.isPlaying;
    public AudioSource Source => _player ? _player.Source : null;

    public void Stop()
    {
        if (_player) _player.StopImmediate();
        _player = null;
    }

    public void SetVolume(float v)
    {
        if (_player && _player.Source) _player.Source.volume = v;
    }

    public void SetPitch(float p)
    {
        if (_player && _player.Source) _player.Source.pitch = p;
    }
}