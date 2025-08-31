using UnityEngine;
using Pathfinding;

public class Enemy : Target
{
    [Header("Enemy Settings")]
    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] private int experienceOnDeath = 1;

    [Header("Contact Damage")]
    [SerializeField] private Damager contactDamager;   // on the same GameObject as Enemy
    [SerializeField] private float contactDamage = 1f;

    [Header("Loot")]
    [SerializeField] private LootTableDefinition lootTable;
    [SerializeField] private Transform lootParent;

    private Transform player;
    private AIPath path;

    private void Start()
    {
        player = PlayerController.Instance?.transform;

        path = GetComponent<AIPath>();
        if (path) path.maxSpeed = moveSpeed;

        if (!contactDamager)
            contactDamager = GetComponent<Damager>() ?? GetComponentInChildren<Damager>(true);

        if (contactDamager)
        {
            // Hit only the Player layer; owner prevents self-hits
            contactDamager.Configure(gameObject, LayerMask.GetMask("Player"), contactDamage);
        }
        else
        {
            Debug.LogWarning($"[Enemy] No Damager found on '{name}'. Contact damage will be disabled.");
        }
    }

    private void Update()
    {
        if (player && path)
            path.destination = player.position;
    }

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
        // If you migrate to the newer LootTableDefinition, change to:
        // if (lootTable) lootTable.TrySpawnDrop(transform.position);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!contactDamager)
            contactDamager = GetComponent<Damager>();
    }
#endif
}