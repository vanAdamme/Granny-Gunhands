using UnityEngine;
using Pathfinding;

public class Enemy : Target
{
    [Header("Enemy Settings")]
    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] private int experienceOnDeath = 1;

    [Header("Contact Damage")]
    [SerializeField] private Damager contactHitbox;
    [SerializeField] private float contactDamage = 1f;

    private Transform player;
    private AIPath path;

    private void Start()
    {
        player = PlayerController.Instance?.transform;
        path = GetComponent<AIPath>();
        path.maxSpeed = moveSpeed;

        // Assign contact hitbox owner/team if set in Inspector
        if (contactHitbox != null)
        {
            contactHitbox.Configure(gameObject, LayerMask.GetMask("Player"), contactDamage);
        }
    }

    private void Update()
    {
        if (player == null) return;

        path.destination = player.position;
    }

    protected override void Die()
    {
        base.Die(); // Sets m_IsDead, deactivates object

        // Enemy-specific death logic
        PlayerController.Instance?.AddExperience(experienceOnDeath);
    }
}