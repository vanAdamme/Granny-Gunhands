using UnityEngine;

/// <summary>
/// Adapter that translates "player entered this room instance"
/// into your encounter flow. Keeps RoomController in charge.
/// </summary>
[DisallowMultipleComponent]
public class RoomEncounterBridge : MonoBehaviour
{
    [SerializeField] private RoomController room;      // optional explicit reference
    [SerializeField] private bool startOnEnter = true; // auto-start encounter
    [SerializeField] private bool onlyOnce = true;     // prevent re-trigger
    private bool fired;

    private void Awake()
    {
        if (!room)
        {
            // Prefer local search; avoids deprecated FindObjectOfType (CS0618)
            room = GetComponent<RoomController>() 
                ?? GetComponentInChildren<RoomController>(true) 
                ?? GetComponentInParent<RoomController>(true);
        }

        if (!room)
            Debug.LogWarning($"[RoomEncounterBridge] No RoomController found on/under '{name}'.");
    }

    public void HandlePlayerEntered(GameObject player)
    {
        if (!startOnEnter || !room) return;
        if (onlyOnce && fired) return;

        fired = true;
        room.BeginEncounter(); // Your existing logic handles doors, spawns, and completion
    }

    public void HandlePlayerExited(GameObject player)
    {
        // Optional: track current room or fade music, etc.
        // Left empty intentionally to keep responsibilities tight.
    }
}