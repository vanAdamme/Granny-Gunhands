using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(menuName = "Audio/Sound Event")]
public class SoundEvent : ScriptableObject
{
    [Tooltip("One will be chosen at random if multiple are provided.")]
    public AudioClip[] clips;

    [Range(0f, 1f)] public float volume = 1f;
    [Tooltip("Base pitch. 1 = normal.")]
    public float pitch = 1f;
    [Tooltip("Random pitch ± range added to base pitch.")]
    public float randomPitchRange = 0f;

    [Tooltip("0 = full 2D (UI), 1 = full 3D (world).")]
    [Range(0f, 1f)] public float spatialBlend = 0f;

    [Tooltip("Optional: limit concurrent instances of this event.")]
    public int maxSimultaneous = 0;  // 0 = unlimited

    [Tooltip("Optional: minimum time between plays for this event.")]
    public float cooldownSeconds = 0f;

    [Tooltip("Optional mixer group routing (SFX/UI).")]
    public AudioMixerGroup mixerGroup;

    [Tooltip("Optional: loop this event (for ambiences).")]
    public bool loop = false;
}