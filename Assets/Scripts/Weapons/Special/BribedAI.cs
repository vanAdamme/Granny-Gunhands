using UnityEngine;
using Pathfinding;

/// Temporarily redirects an enemy to attack other enemies; restores afterwards.
[DisallowMultipleComponent]
public class BribedAI : MonoBehaviour
{
    [Header("Behaviour")]
    [SerializeField] private float searchRadius = 6f;
    [SerializeField] private float reTargetInterval = 0.4f;
    [SerializeField] private bool allowFriendlyFire = true;
    [SerializeField] private float meleeDamageWhileBribed = 1f; // fallback if your Damager damage isn't exposed

    float endsAt, nextRetargetAt;
    Transform decoy;
    AIPath path;
    Transform currentTarget;
    LayerMask enemyLayers;

    Damager[] damagers;

    void Awake()
    {
        path = GetComponent<AIPath>();
        damagers = GetComponentsInChildren<Damager>(includeInactive: true);
    }

    public void Apply(float seconds, Transform decoyTransform, LayerMask enemyLayerMask)
    {
        endsAt = Mathf.Max(endsAt, Time.time + Mathf.Max(0.1f, seconds));
        decoy = decoyTransform;
        enemyLayers = enemyLayerMask;

        if (allowFriendlyFire && damagers != null)
        {
            foreach (var d in damagers)
            {
                if (!d) continue;
                d.Configure(gameObject, LayerMask.GetMask("Enemy"), meleeDamageWhileBribed);
            }
        }

        nextRetargetAt = 0f; // force immediate retarget
    }

    void LateUpdate()
    {
        if (Time.time >= endsAt) { Cleanup(); return; }
        if (!path) return;

        if (Time.time >= nextRetargetAt)
        {
            currentTarget = FindNearestEnemyTarget();
            nextRetargetAt = Time.time + reTargetInterval;
        }

        Vector3 dest = currentTarget ? currentTarget.position : (decoy ? decoy.position : transform.position);
        path.destination = dest;
    }

    Transform FindNearestEnemyTarget()
    {
        var myRoot = transform.root;
        var hits = Physics2D.OverlapCircleAll(transform.position, searchRadius, enemyLayers);

        Transform best = null;
        float bestSqr = float.MaxValue;
        for (int i = 0; i < hits.Length; i++)
        {
            var h = hits[i];
            if (!h) continue;
            var root = h.attachedRigidbody ? h.attachedRigidbody.transform.root : h.transform.root;
            if (root == myRoot) continue;
            if (!root.GetComponent<Enemy>()) continue;

            float sqr = (root.position - transform.position).sqrMagnitude;
            if (sqr < bestSqr) { bestSqr = sqr; best = root; }
        }
        return best;
    }

    void OnDisable() => Cleanup();

    void Cleanup()
    {
        // Restore damagers to hit the Player again
        if (damagers != null)
            foreach (var d in damagers)
                if (d) d.Configure(gameObject, LayerMask.GetMask("Player"), meleeDamageWhileBribed);

        Destroy(this);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, searchRadius);
    }
#endif
}