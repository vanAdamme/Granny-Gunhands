using UnityEngine;

/// <summary>
/// Lives on a room's Floor trigger collider. When the Player enters,
/// notifies the RoomEncounterBridge on the same room instance.
/// </summary>
[DisallowMultipleComponent]
public class RoomEnterTrigger : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    private RoomEncounterBridge bridge; // cached for speed/safety

    public void CacheBridge(RoomEncounterBridge b) => bridge = b;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other || !other.CompareTag(playerTag)) return;
        // In case post-processing didn't cache, resolve on demand:
        if (!bridge) bridge = GetComponentInParent<RoomEncounterBridge>();
        bridge?.HandlePlayerEntered(other.gameObject);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other || !other.CompareTag(playerTag)) return;
        if (!bridge) bridge = GetComponentInParent<RoomEncounterBridge>();
        bridge?.HandlePlayerExited(other.gameObject);
    }
}
