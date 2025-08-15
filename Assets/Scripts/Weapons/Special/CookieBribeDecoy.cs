using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public class CookieBribeDecoy : MonoBehaviour
{
    [Header("Lifetime & Aura")]
    [SerializeField] private float lifetime = 6f;
    [SerializeField] private float auraRadius = 5f;
    [SerializeField] private float retargetDuration = 4f;
    [SerializeField] private LayerMask enemyLayers;

    [Header("VFX (optional)")]
    [SerializeField] private GameObject spawnVFX;
    [SerializeField] private GameObject auraVFX;
    [SerializeField] private GameObject endVFX;

    float despawnAt;

    void Awake()
    {
        var col = GetComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = auraRadius;

        if (spawnVFX) VFX.Spawn(spawnVFX, transform.position, Quaternion.identity, 1.5f);
        if (auraVFX)  VFX.SpawnAttached(auraVFX, transform, transform.position, 1.5f, autoDestroy:false);

        despawnAt = Time.time + lifetime;
    }

    void Update()
    {
        if (Time.time >= despawnAt)
        {
            if (endVFX) VFX.Spawn(endVFX, transform.position, Quaternion.identity, 1.2f);
            Destroy(gameObject);
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        var enemy = other.GetComponentInParent<Enemy>();
        if (!enemy || !enemy.isActiveAndEnabled) return;

        // Layer check on the root
        var root = other.attachedRigidbody ? other.attachedRigidbody.gameObject : other.transform.root.gameObject;
        if ((enemyLayers.value & (1 << root.layer)) == 0) return;

        if (!enemy.TryGetComponent<BribedAI>(out var bribed))
            bribed = enemy.gameObject.AddComponent<BribedAI>();

        bribed.Apply(retargetDuration, transform, enemyLayers);
    }
}