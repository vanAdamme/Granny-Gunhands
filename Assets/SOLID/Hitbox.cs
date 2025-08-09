using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Hitbox : MonoBehaviour
{
    [SerializeField] private float damage = 1f;
    [SerializeField] private bool destroyOnHit = false;
    [SerializeField] private GameObject owner; // enemy, player, etc...
    [SerializeField] private string teamTag;

    private void Awake()
    {
        // Ensure trigger is set so OnTriggerEnter2D works
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Don’t hit self
        if (owner != null && other.gameObject == owner) return;

        // Don't hit same team
        if (!string.IsNullOrEmpty(teamTag) && other.CompareTag(teamTag)) return;

        // Try to damage the thing we hit
        IDamageable damageable = other.GetComponent<IDamageable>();
        if (damageable == null) return;

        damageable.TakeDamage(damage);

        if (destroyOnHit)
        {
            Destroy(gameObject);
        }

    }

    // Allows scripts to set owner dynamically
    public void Configure(GameObject ownerRef, string team, float dmg)
    {
        owner = ownerRef;
        teamTag = team;
        damage = dmg;
    }
}