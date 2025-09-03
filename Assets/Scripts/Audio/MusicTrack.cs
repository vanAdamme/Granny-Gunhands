using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(menuName = "Audio/Music Track")]
public class MusicTrack : ScriptableObject
{
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 1f;
    public AudioMixerGroup mixerGroup;
    public bool loop = true;
}