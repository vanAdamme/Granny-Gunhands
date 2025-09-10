using UnityEngine;

namespace Rooms
{
    [RequireComponent(typeof(Collider2D))]
    public class CurrentRoomTriggerToSignals : MonoBehaviour
    {
        [SerializeField] private CurrentRoomDetectionRoomManager manager;

        void Awake()
        {
            if (!manager)
                manager = GetComponentInParent<CurrentRoomDetectionRoomManager>();
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            if (!manager || manager.RoomInstance == null) return;
            RoomSignals.RaiseEntered(manager.RoomInstance, other.gameObject);
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            if (!manager || manager.RoomInstance == null) return;
            RoomSignals.RaiseExited(manager.RoomInstance, other.gameObject);
        }
    }
}