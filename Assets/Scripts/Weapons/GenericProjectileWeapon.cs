using UnityEngine;

public class GenericProjectileWeapon : Weapon, IUpgradableWeapon
{
    [Header("Definition & Startup")]
    [SerializeField] private WeaponDefinition definitionAsset;
    [SerializeField, Min(1)] private int startLevel = 1;   // visual only (for Level Icons)

    [Header("Pooling")]
    [SerializeField] private UnityPoolService poolService; // optional

    private GameObject ownerRoot;

    [SerializeField] private WeaponRuntimeStats stats;     // runtime copy
    public WeaponRuntimeStats CurrentStats => stats;

    // UI hooks: WeaponItemButton listens to this
    public event System.Action<Sprite> IconChanged;

    protected override void Awake()
    {
        base.Awake();
        if (!poolService) poolService = FindFirstObjectByType<UnityPoolService>();
        ownerRoot = transform.root ? transform.root.gameObject : gameObject;

        if (definitionAsset) SetDefinition(definitionAsset, startLevel);
        RefreshIcon();
    }

    public override void SetDefinition(WeaponDefinition def, int level)
    {
        base.SetDefinition(def, level);         // sets Definition, currentLevel, icon, base cooldown

        // copy base stats from the SO into our runtime block (single source of truth)
        stats.damage                 = def.damage;
        stats.projectileSpeed        = def.projectileSpeed;
        stats.range                  = def.range;
        stats.maxPierces             = def.maxPierces;
        stats.pierceThroughObstacles = def.pierceThroughObstacles;
        stats.cooldown               = Mathf.Max(0.01f, def.baseCooldown);
        CooldownWindow               = stats.cooldown;

        RefreshIcon();                          // notify UI
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

        var evt = def.GetFireSfxForLevel(Level);
        if (evt)
        {
            AudioServicesProvider.Audio.Play(evt, attachTo: muzzle);
        }
    }

    // ---- Manual upgrade API (single interface) ----
    public bool TryApplyUpgrade(WeaponUpgradeDelta d, out string reason)
    {
        reason = "";
        if (d.IsEmpty) { reason = "No stat changes defined."; return false; }

        var before = stats;

        if (d.setDamage.enabled)                 stats.damage = d.setDamage.value;
        if (d.addDamage.enabled)                 stats.damage += d.addDamage.value;

        if (d.setProjectileSpeed.enabled)        stats.projectileSpeed = d.setProjectileSpeed.value;
        if (d.addProjectileSpeed.enabled)        stats.projectileSpeed += d.addProjectileSpeed.value;

        if (d.setRange.enabled)                  stats.range = d.setRange.value;
        if (d.addRange.enabled)                  stats.range += d.addRange.value;

        if (d.setMaxPierces.enabled)             stats.maxPierces = Mathf.Max(0, d.setMaxPierces.value);
        if (d.addMaxPierces.enabled)             stats.maxPierces = Mathf.Max(0, stats.maxPierces + d.addMaxPierces.value);

        if (d.setPierceThroughObstacles.enabled) stats.pierceThroughObstacles = d.setPierceThroughObstacles.value;

        if (d.setCooldown.enabled)               stats.cooldown = Mathf.Max(0.01f, d.setCooldown.value);
        if (d.addCooldown.enabled)               stats.cooldown = Mathf.Max(0.01f, stats.cooldown + d.addCooldown.value);

        CooldownWindow = stats.cooldown;

        bool changed =
            before.damage                 != stats.damage ||
            before.projectileSpeed        != stats.projectileSpeed ||
            before.range                  != stats.range ||
            before.maxPierces             != stats.maxPierces ||
            before.pierceThroughObstacles != stats.pierceThroughObstacles ||
            before.cooldown               != stats.cooldown;

        if (!changed) { reason = "No effective change."; return false; }
        return true;
    }

    private void RefreshIcon()
    {
        Sprite s = Definition ? Definition.GetIconForLevel(Level) : null; // safe if you don't use level icons
        icon = s;
        if (spriteRenderer) spriteRenderer.sprite = s;
        IconChanged?.Invoke(s);  // notify UI
    }
}