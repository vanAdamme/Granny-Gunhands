using UnityEngine;
using Pathfinding; // Aron Granberg A* (IAstarAI)

[DisallowMultipleComponent]
public class BribedAI : MonoBehaviour
{
    private IAstarAI ai;
    private Transform bribeTarget;
    private float bribeUntil;

    public bool IsBribed => bribeTarget != null && Time.time < bribeUntil;

    private void Awake()
    {
        // Try to find IAstarAI on this object or its children
        ai = GetComponent<IAstarAI>();
        if (ai == null) ai = GetComponentInChildren<IAstarAI>();
        enabled = false; // idle until bribed
    }

    /// <summary>Redirect this AI to chase 'target' for 'duration' seconds.</summary>
    public void ApplyBribe(Transform target, float durationSeconds)
    {
        bribeTarget = target;
        bribeUntil = Time.time + durationSeconds;
        enabled = true;
        // Optional: ensure it starts recalculating now
        if (ai != null && bribeTarget != null)
        {
            ai.destination = bribeTarget.position;
            ai.SearchPath();
        }
    }

    /// <summary>Clear the bribe immediately.</summary>
    public void ClearBribe()
    {
        bribeTarget = null;
        bribeUntil = 0f;
        enabled = false;
    }

    private void LateUpdate()
    {
        if (ai == null || bribeTarget == null || Time.time >= bribeUntil)
        {
            ClearBribe();
            return;
        }

        // Win the last-write-wins battle by doing this in LateUpdate
        ai.destination = bribeTarget.position;

        // Refresh path occasionally (cheap check)
        if (!ai.pathPending && ai.reachedEndOfPath)
            ai.SearchPath();
    }

    private void OnDisable()
    {
        // When disabled, stop bribing
        bribeTarget = null;
    }
}