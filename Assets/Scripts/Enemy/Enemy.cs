using UnityEngine;

public class Enemy : Target
{
    [Header("Enemy Settings")]
    [SerializeField] private float moveSpeed = 1f;           // still used by AIPath via a strategy
    [SerializeField] private int experienceOnDeath = 1;

    [Header("Loot")]
    [SerializeField] private LootTableDefinition lootTable;
    [SerializeField] private Transform lootParent;

    // Optional: expose for strategies that want it
    public float MoveSpeed => moveSpeed;

    protected override void Die()
    {
        EnemyEvents.RaiseEnemyKilled();
        DropLoot();
        PlayerController.Instance?.AddExperience(experienceOnDeath);
        base.Die(); // Sets m_IsDead, deactivates object
    }

    private void DropLoot()
    {
        if (!lootTable) return;
        lootTable.TrySpawnLoot(transform.position, lootParent);
    }
}