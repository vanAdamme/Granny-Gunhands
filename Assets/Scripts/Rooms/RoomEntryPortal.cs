using System.Linq;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[AddComponentMenu("Granny/Rooms/Room Entry Portal")]
public class RoomEntryPortal : MonoBehaviour
{
    [Header("Room")]
    [SerializeField] private RoomController room;
    [SerializeField] private PlayerSpawnPoint explicitSpawn;

    [Header("Player Filter")]
    [SerializeField] private LayerMask playerLayers;

    [Header("Behaviour")]
    [SerializeField] private bool autoPickSpawnInRoom = true;

    private bool triggered;

    void Reset() => GetComponent<Collider2D>().isTrigger = true;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered || room == null) return;

        int layer = other.attachedRigidbody ? other.attachedRigidbody.gameObject.layer : other.gameObject.layer;
        if ((playerLayers.value & (1 << layer)) == 0) return;

        var spawner = FindFirstObjectByType<PlayerSpawner>();
        if (spawner == null) { Debug.LogWarning("[RoomEntryPortal] No PlayerSpawner found."); return; }

        var spawn = explicitSpawn ? explicitSpawn : (autoPickSpawnInRoom ? GetBestSpawnInRoom() : null);
        if (spawn) spawner.ForceMoveTo(spawn);

        triggered = true;
        room.BeginEncounter();
    }

    private PlayerSpawnPoint GetBestSpawnInRoom()
    {
        if (!room) return null;
        var points = room.GetComponentsInChildren<PlayerSpawnPoint>(false);
        if (points == null || points.Length == 0) return null;

        var ordered = points.OrderByDescending(p => p.priority).ToArray();
        var safe = ordered.FirstOrDefault(p => p.IsSafe());
        return safe ? safe : ordered.FirstOrDefault();
    }
}