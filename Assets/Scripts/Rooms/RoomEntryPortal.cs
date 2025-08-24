using System.Linq;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[AddComponentMenu("Granny/Rooms/Room Entry Portal")]
public class RoomEntryPortal : MonoBehaviour
{
    [Header("Room")]
    [SerializeField] private RoomController room;               // Room root GO
    [SerializeField] private PlayerSpawnPoint explicitSpawn;    // Optional: force a specific spawn

    [Header("Player Filter")]
    [SerializeField] private LayerMask playerLayers;

    [Header("Behaviour")]
    [Tooltip("Pick highest-priority safe point under Room if no explicit spawn set.")]
    [SerializeField] private bool autoPickSpawnInRoom = true;

    private bool triggered;

    void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered || room == null) return;

        int layer = other.attachedRigidbody ? other.attachedRigidbody.gameObject.layer : other.gameObject.layer;
        if ((playerLayers.value & (1 << layer)) == 0) return;

        var playerSpawner = FindFirstObjectByType<PlayerSpawner>();
        if (playerSpawner == null)
        {
            Debug.LogWarning("[RoomEntryPortal] No PlayerSpawner found in scene.");
            return;
        }

        var spawn = explicitSpawn;
        if (!spawn && autoPickSpawnInRoom)
            spawn = GetBestSpawnInRoom();

        if (spawn)
            playerSpawner.ForceMoveTo(spawn);

        triggered = true;
        room.BeginEncounter();
    }

    private PlayerSpawnPoint GetBestSpawnInRoom()
    {
        if (!room) return null;
        var points = room.GetComponentsInChildren<PlayerSpawnPoint>(includeInactive: false);
        if (points == null || points.Length == 0) return null;

        // Prefer safe → highest priority → first
        var ordered = points.OrderByDescending(p => p.priority).ToArray();
        var safe = ordered.FirstOrDefault(p => p.IsSafe());
        return safe ? safe : ordered.FirstOrDefault();
    }
}