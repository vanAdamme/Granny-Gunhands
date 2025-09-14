using UnityEngine;

public class MeleeAttackStrategy : MonoBehaviour, IAttackStrategy
{
    [SerializeField] private Damager damager;             // your existing hitbox
    [SerializeField] private LayerMask defaultTargetMask; // usually “Player”
    [SerializeField] private float baseDamage = 1f;
    [SerializeField] private float cooldown = 0.6f;

    float nextAt;

    void Reset()
    {
        if (!damager) damager = GetComponent<Damager>();
        if (defaultTargetMask == 0) defaultTargetMask = LayerMask.GetMask("Player");
    }

    void Awake()
    {
        if (!damager) damager = GetComponent<Damager>();
        if (!damager) { Debug.LogWarning($"{name}: No Damager found for MeleeAttackStrategy"); return; }

        // Initial wiring (BribedAI may reconfigure this at runtime to 'Enemy' and back)
        damager.Configure(gameObject, defaultTargetMask, baseDamage);
    }

    public bool TryAttack(IEnemyContext ctx, Transform target, float dt)
    {
        if (!damager || !target) return false;
        if (Time.time < nextAt) return false;

        // Your Damager likely triggers via physics overlap/OnTriggerStay; 
        // here we just drive animation/timing.
        ctx.PlayAnim("Attack");
        nextAt = Time.time + cooldown;
        return true;
    }
}