using UnityEngine;

[DisallowMultipleComponent]
public class RoomTriggerHandler : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    private RoomEncounterBridge bridge;

    void Awake()
    {
        bridge = GetComponentInParent<RoomEncounterBridge>();
        if (!bridge)
            Debug.LogWarning("[RoomTriggerHandler] No RoomEncounterBridge found in parents.");
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
            bridge?.HandlePlayerEntered(other.gameObject);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
            bridge?.HandlePlayerExited(other.gameObject);
    }
}