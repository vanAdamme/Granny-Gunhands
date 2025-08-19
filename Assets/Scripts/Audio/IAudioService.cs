using UnityEngine;

public interface IAudioService
{
    // UI event sounds your code already uses
    AudioSource pause { get; }
    AudioSource unpause { get; }
    AudioSource selectUpgrade { get; }
    AudioSource gameOver { get; }

    // Simple playback API used by GameManager/PlayerController
    void PlaySound(AudioSource source);
    void PlayModifiedSound(AudioSource source, float minPitch = 0.9f, float maxPitch = 1.1f);
}