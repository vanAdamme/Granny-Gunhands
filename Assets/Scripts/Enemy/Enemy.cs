using UnityEngine;
using Pathfinding;
using System;

public class Enemy : Target
{
    [Header("Enemy Settings")]
    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] private int experienceOnDeath = 1;

    [Header("Contact Damage")]
    [SerializeField] private Damager contactDamager;   // on the same GameObject as Enemy
    [SerializeField] private float contactDamage = 1f;

    private Transform player;
    private AIPath path;

    private void Start()
    {
        player = PlayerController.Instance?.transform;
        path = GetComponent<AIPath>();
        if (path) path.maxSpeed = moveSpeed;

        // Prefer Damager on the base object. Fall back to child search only if missing.
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

        base.Die(); // Sets m_IsDead, deactivates object
        PlayerController.Instance?.AddExperience(experienceOnDeath);
    }

#if UNITY_EDITOR
    // Helpful in the editor to auto-wire on prefab changes
    private void OnValidate()
    {
        if (!contactDamager)
            contactDamager = GetComponent<Damager>();
    }
#endif
}