using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RoomController : MonoBehaviour
{
    [Header("Optional: auto-populated if empty")]
    [SerializeField] private List<Door> doors = new();
    [SerializeField] private List<EnemySpawnPoint> spawnPoints = new();

    private readonly List<Health> activeEnemies = new();
    private bool encounterStarted;

    private void Awake()
    {
        if (doors == null || doors.Count == 0)
            doors = GetComponentsInChildren<Door>(true).ToList();

        if (spawnPoints == null || spawnPoints.Count == 0)
            spawnPoints = GetComponentsInChildren<EnemySpawnPoint>(true).ToList();
    }

    public void BeginEncounter()
    {
        if (encounterStarted) return;
        encounterStarted = true;

        foreach (var d in doors) d.Lock();

        foreach (var sp in spawnPoints)
        {
            foreach (var enemy in sp.Spawn())
            {
                if (!enemy) continue;
                activeEnemies.Add(enemy);
                enemy.OnDied += () => OnEnemyDied(enemy);
            }
        }

        if (activeEnemies.Count == 0) UnlockDoors();
    }

    private void OnEnemyDied(Health enemy)
    {
        activeEnemies.Remove(enemy);
        if (activeEnemies.Count == 0) UnlockDoors();
    }

    private void UnlockDoors()
    {
        foreach (var d in doors) d.Unlock();
    }
}