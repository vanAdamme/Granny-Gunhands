using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class RoomEntryTrigger : MonoBehaviour
{
    [SerializeField] private RoomController room;   // usually the room root GO
    [SerializeField] private LayerMask playerLayers;

    private bool triggered;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered || room == null) return;

        int layer = other.gameObject.layer;
        if ((playerLayers.value & (1 << layer)) == 0) return;

        // First time the player enters → start encounter
        triggered = true;
        room.BeginEncounter();
    }
}