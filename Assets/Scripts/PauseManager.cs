using UnityEngine;

public class PauseManager : MonoBehaviour
{
    [SerializeField] bool pauseAudio = true;
    [SerializeField] bool manageCursor = true;

    float savedTimeScale = 1f;
    CursorLockMode savedLock;
    bool savedCursorVisible;

    void OnEnable()  => Pause.OnChanged += Apply;
    void OnDisable() => Pause.OnChanged -= Apply;

    void Apply(bool paused)
    {
        if (paused)
        {
            savedTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            if (pauseAudio) AudioListener.pause = true;

            if (manageCursor)
            {
                savedLock = Cursor.lockState;
                savedCursorVisible = Cursor.visible;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
        else
        {
            Time.timeScale = savedTimeScale <= 0f ? 1f : savedTimeScale;
            if (pauseAudio) AudioListener.pause = false;

            if (manageCursor)
            {
                Cursor.lockState = savedLock;
                Cursor.visible = savedCursorVisible;
            }
        }
    }
}