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
    Debug.Log($"[RoomEncounterOnEnter] Subscribing on {name}", this);
    RoomSignals.RoomEntered += OnRoomEntered;
}
void OnDisable()
{
    RoomSignals.RoomEntered -= OnRoomEntered;
}

private void OnRoomEntered(RoomInstanceGrid2D entered, GameObject player)
{
    var mgr = Manager;
    Debug.Log($"[RoomEncounterOnEnter] Heard Entered. entered={entered} mgr={(mgr ? mgr.RoomInstance : null)} onlyOnce={onlyOnce} fired={fired}", this);

    if (!mgr) { Debug.LogWarning("[RoomEncounterOnEnter] No Manager resolved.", this); return; }
    if (entered != mgr.RoomInstance) { Debug.Log($"[RoomEncounterOnEnter] Not my room.", this); return; }
    if (onlyOnce && fired) { Debug.Log("[RoomEncounterOnEnter] Already fired.", this); return; }

    fired = true;
    Debug.Log("[RoomEncounterOnEnter] BEGIN ENCOUNTER", this);
    room?.BeginEncounter();
}
    }
}