using UnityEngine;

/// <summary>
/// Generic projectile weapon that spawns a projectile prefab from WeaponDefinition
/// and injects owner + ISpecialCharge so hits can charge specials.
/// Each instance handles its own cooldown (left/right click independence). 
/// </summary>
public class GenericProjectileWeapon : Weapon
{
    [Header("Charge Injection")]
    [Tooltip("Usually the SpecialChargeSimple on the player root.")]
    [SerializeField] private MonoBehaviour specialChargeSource; // ISpecialCharge
    private ISpecialCharge charge;

    [Header("Optional dual muzzle setup")]
    [SerializeField] private Transform secondaryMuzzle;
    [SerializeField] private bool alternateMuzzles = true;
    private bool usePrimaryNext = true;

    private Transform ownerRoot;

    protected override void Awake()
    {
        base.Awake();

        ownerRoot = transform.root;

        // Resolve charge via serialized ref first; safe fallback using Unity 6 API.
        charge = specialChargeSource as ISpecialCharge;
        if (charge == null)
        {
            var c = Object.FindFirstObjectByType<SpecialChargeSimple>();
            if (c) charge = c;
        }
    }

    public override bool TryFire(Vector2 dir) => base.TryFire(dir); // keep base contract (returns bool)  :contentReference[oaicite:1]{index=1}

    protected override void Shoot(Vector2 dir)
    {
        if (!Definition)
        {
            Debug.LogWarning($"[{name}] No WeaponDefinition assigned.", this);
            return;
        }
        if (!Definition.projectilePrefab)
        {
            Debug.LogWarning($"[{name}] Definition '{Definition.DisplayName}' has no projectile prefab.", this);
            return;
        }

        // choose muzzle
        Transform muzz = muzzle;
        if (alternateMuzzles && secondaryMuzzle)
        {
            muzz = usePrimaryNext ? muzzle : secondaryMuzzle;
            usePrimaryNext = !usePrimaryNext;
        }
        if (!muzz)
        {
            Debug.LogWarning($"[{name}] No muzzle assigned.", this);
            return;
        }

        // per-level stats from definition
        var stats = Definition.GetStatsForLevel(Level); // cooldown, speed, range, maxPierces, etc.  :contentReference[oaicite:2]{index=2}

        // derive TTL from range/speed
        float speed = Mathf.Max(0.01f, stats.projectileSpeed);
        float ttl   = Mathf.Max(0.01f, stats.range / speed);

        // Instantiate projectile (pool later).
        var go = Instantiate(Definition.projectilePrefab, muzz.position,
                             Quaternion.FromToRotation(Vector2.right, dir.normalized));

        var proj = go.GetComponent<Projectile>();
        if (!proj)
        {
            Debug.LogError($"[{name}] The projectile prefab on '{Definition.DisplayName}' has no Projectile component.", go);
            Destroy(go);
            return;
        }

        // Configure masks from definition
        var hitMask   = Definition.targetLayers;
        var blockMask = Definition.obstacleLayers;
        bool canPierceObstacles = stats.pierceThroughObstacles;

        // Convert "maxPierces" (extra pierces) → allowed unique hits (include first hit)
        int allowedUniqueHits = Mathf.Max(1, 1 + stats.maxPierces);

        proj.Initialize(ownerRoot,
                        charge,
                        dir,
                        speed,
                        ttl,
                        allowedUniqueHits,
                        hitMask,
                        blockMask,
                        canPierceObstacles);

        // VFX (optional)
        if (Definition.muzzleFlashPrefab)
        {
            var fx = Instantiate(Definition.muzzleFlashPrefab, muzz.position, muzz.rotation);
            Destroy(fx, 1.5f);
        }

        // SFX: Removed AudioService.PlayOneShot call since your AudioService lacks this API.
        // If your SoundEvent can play itself, you can uncomment one of these patterns:
        // Definition.GetFireSfxForLevel(Level)?.PlayAtPosition(muzz.position);
        // or route through your actual audio layer.
    }

    public override void SetDefinition(WeaponDefinition def, int level)
    {
        base.SetDefinition(def, level);   // sets icon + CooldownWindow from def.baseCooldown  :contentReference[oaicite:3]{index=3}
        if (!Definition) return;

        // Prefer per-level cooldown when defined (keeps Weapon.CooldownWindow authoritative)
        var stats = Definition.GetStatsForLevel(Level); // has 'cooldown'  :contentReference[oaicite:4]{index=4}
        CooldownWindow = Mathf.Max(0.01f, stats.cooldown);
    }
}