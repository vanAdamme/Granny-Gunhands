// GenericProjectileWeapon.cs
using UnityEngine;

public class GenericProjectileWeapon : Weapon, IUpgradableWeaponV2
{
    [Header("Definition & Startup")]
    [SerializeField] private WeaponDefinition definitionAsset;
    [SerializeField, Min(1)] private int startLevel = 1; // keep if your icon per-level matters

    [Header("Pooling")]
    [SerializeField] private UnityPoolService poolService;

    private GameObject ownerRoot;
    [SerializeField] private WeaponRuntimeStats stats;   // runtime copy
    public WeaponRuntimeStats CurrentStats => stats;

    public event System.Action<Sprite> IconChanged;

    protected override void Awake()
    {
        base.Awake();
        if (!poolService) poolService = FindFirstObjectByType<UnityPoolService>();
        ownerRoot = transform.root ? transform.root.gameObject : gameObject;

        if (definitionAsset) SetDefinition(definitionAsset, startLevel);
        UpdateRuntimeIcon();
    }

    public override void SetDefinition(WeaponDefinition def, int level)
    {
        base.SetDefinition(def, level);          // sets Definition + CooldownWindow from def.baseCooldown :contentReference[oaicite:2]{index=2}
        // Copy base stats from the definition (single source of truth) :contentReference[oaicite:3]{index=3}
        stats.damage                 = def.damage;
        stats.projectileSpeed        = def.projectileSpeed;
        stats.range                  = def.range;
        stats.maxPierces             = def.maxPierces;
        stats.pierceThroughObstacles = def.pierceThroughObstacles;
        stats.cooldown               = Mathf.Max(0.01f, def.baseCooldown);
        CooldownWindow               = stats.cooldown;   // keep Weapon’s cooldown in sync
    }

    protected override void Shoot(Vector2 dir)
    {
        var def = Definition;
        if (!def || !def.projectilePrefab) return;

        Vector3 pos = muzzle ? muzzle.position : transform.position;
        GameObject go = poolService
            ? poolService.Spawn(def.projectilePrefab, pos, Quaternion.identity)
            : Instantiate(def.projectilePrefab, pos, Quaternion.identity);

        go.transform.right = dir;
        var proj = go.GetComponent<Projectile>() ?? go.AddComponent<Projectile>();

        // Use runtime stats (not the SO) for firing
        proj.Init(ownerRoot, def.targetLayers, stats.damage, dir);
        proj.SetRuntime(
            speedOverride: stats.projectileSpeed,
            rangeOverride: stats.range,
            obstacleOverride: def.obstacleLayers,
            maxPiercesOverride: stats.maxPierces,
            pierceObstaclesOverride: stats.pierceThroughObstacles,
            radiusOverride: null,
            vfxOverride: null
        );

        if (def.muzzleFlashPrefab && muzzle)
        {
            if (poolService) poolService.Spawn(def.muzzleFlashPrefab, muzzle.position, muzzle.rotation);
            else Destroy(Instantiate(def.muzzleFlashPrefab, muzzle.position, muzzle.rotation), 0.15f);
        }
    }

    public bool TryApplyUpgrade(WeaponUpgradeDelta d, out string reason)
    {
        reason = "";
        if (d.IsEmpty) { reason = "Empty upgrade."; return false; }

        var before = stats;

        if (d.setDamage.HasValue)            stats.damage = d.setDamage.Value;
        if (d.addDamage.HasValue)            stats.damage += d.addDamage.Value;

        if (d.setProjectileSpeed.HasValue)   stats.projectileSpeed = d.setProjectileSpeed.Value;
        if (d.addProjectileSpeed.HasValue)   stats.projectileSpeed += d.addProjectileSpeed.Value;

        if (d.setRange.HasValue)             stats.range = d.setRange.Value;
        if (d.addRange.HasValue)             stats.range += d.addRange.Value;

        if (d.setMaxPierces.HasValue)        stats.maxPierces = Mathf.Max(0, d.setMaxPierces.Value);
        if (d.addMaxPierces.HasValue)        stats.maxPierces = Mathf.Max(0, stats.maxPierces + d.addMaxPierces.Value);

        if (d.setPierceThroughObstacles.HasValue) stats.pierceThroughObstacles = d.setPierceThroughObstacles.Value;

        if (d.setCooldown.HasValue)          stats.cooldown = Mathf.Max(0.01f, d.setCooldown.Value);
        if (d.addCooldown.HasValue)          stats.cooldown = Mathf.Max(0.01f, stats.cooldown + d.addCooldown.Value);

        CooldownWindow = stats.cooldown;

        bool changed =
            before.damage != stats.damage ||
            before.projectileSpeed != stats.projectileSpeed ||
            before.range != stats.range ||
            before.maxPierces != stats.maxPierces ||
            before.pierceThroughObstacles != stats.pierceThroughObstacles ||
            before.cooldown != stats.cooldown;

        if (!changed) reason = "No effective change.";
        return changed;
    }

    private void UpdateRuntimeIcon()
    {
        Sprite s = Definition ? Definition.GetIconForLevel(Level) : null;
        if (s != icon)
        {
            icon = s;
            IconChanged?.Invoke(icon);
        }
        if (spriteRenderer) spriteRenderer.sprite = s;
    }
}