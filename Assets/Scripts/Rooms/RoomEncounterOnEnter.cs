using UnityEngine;
using Edgar.Unity;

namespace Rooms
{
    [DisallowMultipleComponent]
    public class RoomEncounterOnEnter : MonoBehaviour
    {
        [SerializeField] private RoomController room;
        [SerializeField] private bool onlyOnce = true;
        private bool fired;

        private CurrentRoomDetectionRoomManager _mgr;
        private CurrentRoomDetectionRoomManager Manager
            => _mgr ? _mgr : (_mgr = GetComponent<CurrentRoomDetectionRoomManager>() 
                                     ?? GetComponentInParent<CurrentRoomDetectionRoomManager>());

        void Awake()
        {
            if (!room)
                room = GetComponent<RoomController>() ?? GetComponentInChildren<RoomController>(true);
        }

        void OnEnable()
        {
            RoomSignals.RoomEntered += OnRoomEntered;
        }
        void OnDisable()
        {
            RoomSignals.RoomEntered -= OnRoomEntered;
        }

        private void OnRoomEntered(RoomInstanceGrid2D entered, GameObject player)
        {
            var mgr = Manager;
 
            if (!mgr) { return; }
            if (entered != mgr.RoomInstance) { return; }
            if (onlyOnce && fired) { return; }

            fired = true;
            room?.BeginEncounter();
        }
    }
}