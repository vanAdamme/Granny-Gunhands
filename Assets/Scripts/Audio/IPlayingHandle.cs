using UnityEngine;

public interface IPlayingHandle
{
    bool IsPlaying { get; }
    AudioSource Source { get; }     // expose for advanced cases
    void Stop();
    void SetVolume(float v);
    void SetPitch(float p);
}