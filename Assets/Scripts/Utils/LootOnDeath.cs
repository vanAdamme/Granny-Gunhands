using UnityEngine;

[DisallowMultipleComponent]
public class LootOnDeath : MonoBehaviour
{
    [SerializeField] private Health health;
    [SerializeField] private LootTableDefinition lootTable;

    void Reset() => health = GetComponent<Health>();

    void Awake()
    {
        if (!health) health = GetComponent<Health>();
        if (health) health.OnDied += HandleDeath;
    }

    void OnDestroy()
    {
        if (health) health.OnDied -= HandleDeath;
    }

    private void HandleDeath()
    {
        if (!lootTable) return;
        lootTable.TrySpawnDrop(transform.position); // rolls + spawns pickups (weapons, powerups, prefabs)
    }
}