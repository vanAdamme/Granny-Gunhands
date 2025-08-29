using UnityEngine;

/// Simple projectile weapon that pulls display/state from WeaponDefinition,
/// supports upgrades, and spawns projectiles via a central pooling service.
public class GenericProjectileWeapon : Weapon, IUpgradableWeapon
{
    [Header("Definition & Startup")]
    [SerializeField] private WeaponDefinition definitionAsset;
    [SerializeField, Min(1)] private int startLevel = 1;

    [Header("Upgrade Caps")]
    [SerializeField, Min(1)] private int maxLevel = 5;

    [Header("Per-Level Gains")]
    [SerializeField] private float damagePerLevel = 2f;
    [SerializeField] private float projectileSpeedPerLevel = 0.5f;
    [SerializeField] private float rangePerLevel = 0f;
    [SerializeField] private float piercesPerLevel = 0f; // fractional allowed; accrues, rounds down

    [Header("Pooling")]
    [SerializeField] private UnityPoolService poolService; // scene service; optional (falls back to Instantiate)

    // Current upgrade level (1-based)
    [SerializeField, Min(1)] private int level = 1;

    // Who owns the projectiles (used to mark Damager/source)
    private GameObject ownerRoot;

    // Runtime icon change notification (UI listens for this)
    public event System.Action<Sprite> IconChanged;

    protected override void Awake()
    {
        base.Awake();

        if (!poolService) poolService = FindFirstObjectByType<UnityPoolService>();
        ownerRoot = transform.root ? transform.root.gameObject : gameObject;

        if (definitionAsset)
        {
            SetDefinition(definitionAsset, startLevel);
            level = Mathf.Max(1, startLevel);
        }

        UpdateRuntimeIcon();
    }

    protected override void Shoot(Vector2 dir)
    {
        var def = Definition;
        if (!def || !def.projectilePrefab) return;

        Vector3 pos = muzzle ? muzzle.position : transform.position;

        // Spawn projectile from pool (or Instantiate as graceful fallback)
        GameObject go = poolService
            ? poolService.Spawn(def.projectilePrefab, pos, Quaternion.identity)
            : Instantiate(def.projectilePrefab, pos, Quaternion.identity);

        go.transform.right = dir;

        var proj = go.GetComponent<Projectile>();
        if (!proj) proj = go.AddComponent<Projectile>();

        // Minimal init + runtime overrides
        proj.Init(ownerRoot, def.targetLayers, def.damage, dir);
        proj.SetRuntime(
            speedOverride: def.projectileSpeed,
            rangeOverride: def.range,
            obstacleOverride: def.obstacleLayers,
            maxPiercesOverride: def.maxPierces,
            pierceObstaclesOverride: def.pierceThroughObstacles,
            radiusOverride: null // no vfx override here; WeaponDefinition has no hitVFXPrefab
        );

        // Muzzle flash (make sure prefab has ReturnToPoolAfterSeconds)
        if (def.muzzleFlashPrefab && muzzle)
        {
            if (poolService) poolService.Spawn(def.muzzleFlashPrefab, muzzle.position, muzzle.rotation);
            else Destroy(Instantiate(def.muzzleFlashPrefab, muzzle.position, muzzle.rotation), 0.15f);
        }
    }

    // === Upgrade plumbing =================================================

    public bool TryPreviewUpgrade(int levels, out UpgradeDelta delta, out string reason)
    {
        delta = default; reason = "";

        int target  = Mathf.Min(maxLevel, level + Mathf.Max(0, levels));
        int applied = target - level;
        if (applied <= 0) { reason = "Already max level."; return false; }

        delta.damage          = applied * damagePerLevel;
        delta.projectileSpeed = applied * projectileSpeedPerLevel;
        delta.range           = applied * rangePerLevel;

        // fractional pierce accrual → only increments when a whole is earned
        int startWhole = Mathf.FloorToInt((level  - 1) * piercesPerLevel);
        int endWhole   = Mathf.FloorToInt((target - 1) * piercesPerLevel);
        int pierceGain = Mathf.Max(0, endWhole - startWhole);
        delta.pierces = pierceGain;

        return !delta.IsEmpty;
    }

    public bool TryApplyUpgrade(int levels, out int appliedLevels, out string reason)
    {
        appliedLevels = 0;
        if (!TryPreviewUpgrade(levels, out var d, out reason)) return false;

        var def = Definition;
        if (!def) { reason = "No definition."; return false; }

        int target = Mathf.Min(maxLevel, level + Mathf.Max(0, levels));
        appliedLevels = target - level;
        level = target;

        def.damage          += d.damage;
        def.projectileSpeed += d.projectileSpeed;
        def.range           += d.range;
        def.maxPierces      += d.pierces;   // only increases when a whole was earned

        UpdateRuntimeIcon();
        return appliedLevels > 0;
    }

    private void UpdateRuntimeIcon()
    {
        Sprite s = Definition ? Definition.GetIconForLevel(level) : null;
        if (s != icon)
        {
            icon = s;
            IconChanged?.Invoke(icon);
        }
        if (spriteRenderer) spriteRenderer.sprite = s;
    }
}
