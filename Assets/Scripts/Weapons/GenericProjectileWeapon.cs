using UnityEngine;

/// <summary>
/// Spawns projectiles from one or two muzzles, injecting the owner's ISpecialCharge so hits
/// can charge specials. Keeps weapon stats separate: WeaponDefinition provides designer data,
/// WeaponRuntimeStats represents the resolved numbers at runtime (rate, spread, pierce, etc.).
/// </summary>
public class GenericProjectileWeapon : Weapon
{
    [Header("Refs")]
    [Tooltip("Projectile prefab that contains a Projectile component.")]
    [SerializeField] private Projectile projectilePrefab;
    [Tooltip("Primary muzzle transform (left hand, etc.).")]
    [SerializeField] private Transform primaryMuzzle;
    [Tooltip("Optional secondary muzzle (right hand, dual-wield).")]
    [SerializeField] private Transform secondaryMuzzle;

    [Header("Stats")]
    [SerializeField] private WeaponDefinition definition;     // Design-time config (spread, base damage, etc.)
    [SerializeField] private WeaponRuntimeStats runtimeStats; // Live, modified stats (fire rate, damage, speed...)

    [Header("Charge Injection")]
    [Tooltip("Source that implements ISpecialCharge (usually on the Player root).")]
    [SerializeField] private MonoBehaviour specialChargeSource; // SpecialChargeSimple
    private ISpecialCharge charge;

    [Header("Firing")]
    [SerializeField] private bool alternateMuzzles = true;
    [SerializeField] private LayerMask projectileHitMask;
    [SerializeField] private LayerMask obstructionMask;

    private float nextFireTime;
    private bool usePrimaryNext = true;
    private Transform ownerRoot;

    protected override void Awake()
    {
        base.Awake();

        if (!projectilePrefab)
            Debug.LogError($"[{name}] Missing projectilePrefab.", this);

        // Resolve charge via serialized reference or a safe fallback
        charge = specialChargeSource as ISpecialCharge;
        if (charge == null)
        {
            var c = Object.FindFirstObjectByType<SpecialChargeSimple>();
            if (c != null) charge = c;
        }

        // Cache owner root for self-hit filtering in projectiles
        ownerRoot = transform.root;

        // If runtime stats aren’t injected elsewhere, create one from definition at runtime
        if (runtimeStats == null)
        {
            runtimeStats = new WeaponRuntimeStats();
            if (definition != null)
                runtimeStats.ApplyDefinition(definition);
        }
    }

    private void OnValidate()
    {
        // Keep layers sane while editing
        if (projectileHitMask == 0)
        {
            // Assuming an "Enemies" layer commonly exists in your project
            int enemies = LayerMask.NameToLayer("Enemies");
            if (enemies >= 0) projectileHitMask = (1 << enemies);
        }
    }

    /// <summary>
    /// Called by PlayerShooting / inventory systems to request a shot.
    /// </summary>
    public override void TryFire(Vector2 aimDirection)
    {
        if (!enabled || projectilePrefab == null) return;

        // Fire-rate gate
        var t = Time.time;
        if (t < nextFireTime) return;
        nextFireTime = t + Mathf.Max(0.01f, runtimeStats.FireInterval);

        // Which muzzle?
        var muzzle = primaryMuzzle;
        if (alternateMuzzles && secondaryMuzzle)
        {
            muzzle = usePrimaryNext ? primaryMuzzle : secondaryMuzzle;
            usePrimaryNext = !usePrimaryNext;
        }

        if (!muzzle)
        {
            Debug.LogWarning($"[{name}] No muzzle assigned for shot.", this);
            return;
        }

        // Spread
        var dir = aimDirection.sqrMagnitude > 0.0001f ? aimDirection.normalized : muzzle.right;
        var spreadRad = runtimeStats.GetRandomSpreadRadians();
        dir = Rotate(dir, spreadRad).normalized;

        SpawnProjectile(muzzle.position, dir);
        PlayMuzzleFx(muzzle);
        OnFired?.Invoke(this);
    }

    private void SpawnProjectile(Vector3 position, Vector2 direction)
    {
        // Instantiate (pool later as needed)
        var proj = Instantiate(projectilePrefab, position, Quaternion.FromToRotation(Vector2.right, direction));

        // Configure masks
        proj.SetHitMask(projectileHitMask);
        proj.SetObstructionMask(obstructionMask);

        // Configure runtime values
        proj.SetSpeed(runtimeStats.ProjectileSpeed);
        proj.SetLifetime(runtimeStats.ProjectileLifetime);
        proj.SetPierce(Mathf.Max(1, runtimeStats.PierceCount));

        // Inject owner + special charge
        proj.Initialize(ownerRoot, charge, direction, runtimeStats.ProjectileSpeed, runtimeStats.ProjectileLifetime, runtimeStats.PierceCount);
    }

    private void PlayMuzzleFx(Transform muzzle)
    {
        if (definition == null) return;
        if (definition.muzzleVFXPrefab)
        {
            var vfx = Instantiate(definition.muzzleVFXPrefab, muzzle.position, muzzle.rotation);
            Destroy(vfx, 1.5f);
        }
        if (definition.muzzleSFX)
        {
            // Route to your audio service if present. Kept optional to avoid coupling.
            var audio = Object.FindFirstObjectByType<AudioService>();
            audio?.PlayOneShot(definition.muzzleSFX, muzzle.position);
        }
    }

    // Small helper for spread
    private static Vector2 Rotate(Vector2 v, float rads)
    {
        var s = Mathf.Sin(rads);
        var c = Mathf.Cos(rads);
        return new Vector2(c * v.x - s * v.y, s * v.x + c * v.y);
    }
}