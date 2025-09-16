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
        if (m_IsDead) return;

        EnemyEvents.RaiseEnemyKilled();
        DropLoot();
        PlayerController.Instance?.AddExperience(experienceOnDeath);

        var anim = GetComponent<Animator>();
        if (anim) anim.SetTrigger("Die");

        base.Die(); // <- raises OnDied and marks dead; no auto-disable in Health anymore
    }

    private void DropLoot()
    {
        if (!lootTable) return;
        lootTable.TrySpawnLoot(transform.position, lootParent);
    }
}