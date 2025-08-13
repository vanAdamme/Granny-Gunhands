using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider2D))]
public class DamageZone : MonoBehaviour
{
    [Header("Tick Settings")]
    [SerializeField] float damagePerTick = 1f;
    [SerializeField, Min(0f)] float tickInterval = 1f;
    [SerializeField] bool tickOnEnter = false;

    [Header("Targeting")]
    [SerializeField] LayerMask targetLayers;  // set to Player layer (or whatever) in Inspector

    [Header("One-Shot Mode")]
    [SerializeField] bool onceOnly = false;           // if true, zone disables after first successful tick
    [SerializeField] bool disableGameObject = false;  // if true, SetActive(false); else just disable component

    [Header("FX")]
    [SerializeField] GameObject tickVfxPrefab;   // optional; spawned at target root
    [SerializeField, Min(0f)] float vfxLifetime = 1f;
    [SerializeField] Vector3 vfxOffset = Vector3.zero;

    [SerializeField] AudioClip tickSfx;          // optional; plays on tick
    [SerializeField] AudioSource audioSource;    // optional; if null we’ll try AudioController (if you use one)

    [Header("Events")]
    public UnityEvent onTick;                    // raised after damage is applied

    // runtime
    IDamageable target;
    Health targetHealth;
    Transform targetRoot;
    float timer;

    void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var root = other.attachedRigidbody ? other.attachedRigidbody.transform.root : other.transform.root;
        if (!IsInLayerMask(root.gameObject.layer, targetLayers)) return;

        target      = root.GetComponentInChildren<IDamageable>();
        targetHealth= root.GetComponentInChildren<Health>();
        targetRoot  = root;

        if (target == null) return;

        timer = tickOnEnter ? 0f : tickInterval;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        var root = other.attachedRigidbody ? other.attachedRigidbody.transform.root : other.transform.root;
        if (!IsInLayerMask(root.gameObject.layer, targetLayers)) return;

        if (root == targetRoot)
        {
            target = null;
            targetHealth = null;
            targetRoot = null;
        }
    }

    void Update()
    {
        if (target == null) return;

        timer -= Time.deltaTime;
        if (timer > 0f) return;

        // respect i-frames: wait until invulnerability ends, then tick immediately
        if (targetHealth != null && targetHealth.IsInvulnerable) return;

        DoTick();
        timer = tickInterval;

        if (onceOnly)
        {
            if (disableGameObject) gameObject.SetActive(false);
            else enabled = false;
        }
    }

    void DoTick()
    {
        // Damage
        target.TakeDamage(damagePerTick);

        // VFX
        if (tickVfxPrefab && targetRoot)
        {
            var v = Instantiate(tickVfxPrefab, targetRoot.position + vfxOffset, Quaternion.identity);
            if (vfxLifetime > 0f) Destroy(v, vfxLifetime);
        }

        // SFX (prefer project AudioSource; otherwise try your AudioController singleton if you have one)
        if (tickSfx)
        {
            if (audioSource) audioSource.PlayOneShot(tickSfx);
            else
            {
                // Optional integration with your existing AudioController
                // AudioController.Instance?.PlaySound(tickSfx);
            }
        }

        // Event
        onTick?.Invoke();
    }

    static bool IsInLayerMask(int layer, LayerMask mask) => (mask.value & (1 << layer)) != 0;

    #if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        var c = GetComponent<Collider2D>();
        if (c) Gizmos.DrawWireCube(c.bounds.center, c.bounds.size);
    }
    #endif
}