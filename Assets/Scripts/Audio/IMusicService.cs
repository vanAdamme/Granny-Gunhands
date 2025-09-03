public interface IMusicService
{
    void Play(MusicTrack track, float fadeSeconds = 1.0f, bool loop = true);
    void Stop(float fadeSeconds = 0.5f);
}