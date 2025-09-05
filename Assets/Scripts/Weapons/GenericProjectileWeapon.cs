using UnityEngine;

public class GenericProjectileWeapon : Weapon
{
    [SerializeField] private WeaponDefinition defaultDefinition;

    [Header("Charge Injection")]
    [Tooltip("Drag the player's SpecialChargeSimple here.")]
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

        if (!Definition && defaultDefinition)
            SetDefinition(defaultDefinition, currentLevel);

        ownerRoot = transform.root;

        // Resolve charge with strong preference for the owner's hierarchy
        charge = specialChargeSource as ISpecialCharge
            ?? GetComponentInParent<SpecialChargeSimple>()   // ← anchor to Player
            ?? Object.FindFirstObjectByType<SpecialChargeSimple>();
    }

    public override bool TryFire(Vector2 dir) => base.TryFire(dir);

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

        var stats = Definition.GetStatsForLevel(Level);

        // derive TTL from range/speed
        float speed = Mathf.Max(0.01f, stats.projectileSpeed);
        float ttl   = Mathf.Max(0.01f, stats.range / Mathf.Max(0.01f, speed));

        // spawn projectile
        var go = Instantiate(Definition.projectilePrefab, muzz.position,
                             Quaternion.FromToRotation(Vector2.right, dir.normalized));

        var proj = go.GetComponent<Projectile>();
        if (!proj)
        {
            Debug.LogError($"[{name}] The projectile prefab on '{Definition.DisplayName}' has no Projectile component.", go);
            Destroy(go);
            return;
        }

        // configure masks
        var hitMask   = Definition.targetLayers;
        var blockMask = Definition.obstacleLayers;
        bool canPierceObstacles = stats.pierceThroughObstacles;
        int allowedUniqueHits   = Mathf.Max(1, 1 + stats.maxPierces);

        // inject player charge + damage for DAMAGE-BASED charging
        proj.Initialize(ownerRoot,
                        charge,
                        dir,
                        speed,
                        ttl,
                        allowedUniqueHits,
                        hitMask,
                        blockMask,
                        canPierceObstacles,
                        dmg: stats.damage); // ← pass damage so projectile can actually deal it

        // optional VFX
        if (Definition.muzzleFlashPrefab)
        {
            var fx = Instantiate(Definition.muzzleFlashPrefab, muzz.position, muzz.rotation);
            Destroy(fx, 1.5f);
        }
    }

    public override void SetDefinition(WeaponDefinition def, int level)
    {
        base.SetDefinition(def, level);
        if (!Definition) return;

        var stats = Definition.GetStatsForLevel(Level);
        CooldownWindow = Mathf.Max(0.01f, stats.cooldown);
    }
}