using UnityEngine;

public class LevelMusic : MonoBehaviour
{
    [SerializeField] private MusicTrack dungeonLoop;

    void Start() => AudioServicesProvider.Music.Play(dungeonLoop, fadeSeconds: 1.0f, loop: true);
}