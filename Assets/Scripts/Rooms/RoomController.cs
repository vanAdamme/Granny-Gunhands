using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RoomController : MonoBehaviour
{
    [Header("Optional: will auto-populate if left empty")]
    [SerializeField] private List<Door> doors = new();
    [SerializeField] private List<EnemySpawnPoint> spawnPoints = new();

    private readonly List<Health> activeEnemies = new();
    private bool encounterStarted;

    private void Awake()
    {
        // Auto-find if not wired in prefab
        if (doors == null || doors.Count == 0)
            doors = GetComponentsInChildren<Door>(includeInactive: true).ToList();

        if (spawnPoints == null || spawnPoints.Count == 0)
            spawnPoints = GetComponentsInChildren<EnemySpawnPoint>(includeInactive: true).ToList();
    }

    public void BeginEncounter()
    {
        if (encounterStarted) return;
        encounterStarted = true;

        // 1) Lock all doors
        foreach (var d in doors) d.Lock();

        // 2) Spawn enemies and register for deaths
        foreach (var sp in spawnPoints)
        {
            foreach (var enemy in sp.Spawn())
            {
                if (!enemy) continue;
                activeEnemies.Add(enemy);
                enemy.OnDied += () => OnEnemyDied(enemy);
            }
        }

        // If a room has no spawners/enemies, unlock instantly
        if (activeEnemies.Count == 0)
            UnlockDoors();
    }

    private void OnEnemyDied(Health enemy)
    {
        activeEnemies.Remove(enemy);
        if (activeEnemies.Count == 0)
            UnlockDoors();
    }

    private void UnlockDoors()
    {
        foreach (var d in doors) d.Unlock();
    }
}