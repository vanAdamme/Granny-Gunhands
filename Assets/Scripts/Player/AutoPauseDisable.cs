using UnityEngine;

public class AutoPauseDisable : MonoBehaviour
{
    [SerializeField] private MonoBehaviour[] targets;

    void OnEnable()
    {
        Pause.OnChanged += OnPauseChanged;
        OnPauseChanged(Pause.IsPaused); // apply current state
    }

    void OnDisable() => Pause.OnChanged -= OnPauseChanged;

    void OnPauseChanged(bool paused)
    {
        foreach (var t in targets)
            if (t) t.enabled = !paused;
    }
}