using UnityEngine;

public interface IAudioService
{
    // Fire-and-forget UI/SFX. Returns an instance handle if you need control.
    IPlayingHandle Play(SoundEvent evt, Vector3? worldPos = null, Transform attachTo = null);

    // Convenience: 2D UI sounds (auto-routed to UI mixer, spatialBlend = 0)
    IPlayingHandle PlayUI(SoundEvent evt);

    // Stop all currently playing instances of a given event.
    void StopAll(SoundEvent evt);

    // Global control via mixer exposed params (names are data, not code).
    void SetBusVolume(string exposedParam, float linear01);
}