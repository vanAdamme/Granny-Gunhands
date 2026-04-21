using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Loot/Loot Table Definition")]
public class LootTableDefinition : ScriptableObject
{
    [Header("Config")]
    [SerializeField] private RaritySettings raritySettings;

    // Prefab bundle passed to Entry.TrySpawn so the selection loop stays type-agnostic.
    public readonly struct SpawnPrefabs
    {
        public readonly WeaponPickup weapon;
        public readonly PowerUpPickup powerUp;
        public readonly WeaponUpgradePickup upgrade;

        public SpawnPrefabs(WeaponPickup w, PowerUpPickup pu, WeaponUpgradePickup up)
        { weapon = w; powerUp = pu; upgrade = up; }
    }

    [Serializable]
    public class Entry
    {
        [Header("Pick ONE payload")]
        public WeaponDefinition weaponDef;
        public PowerUpDefinition powerUpDef;
        public WeaponUpgradeItemDefinition upgradeItemDef;
        public GameObject prefab; // any arbitrary prefab

        [Header("Overrides (0 = use rarity defaults)")]
        [Range(0f, 1f)] public float dropChanceOverride;
        public int weightOverride;

        public bool HasWeapon  => weaponDef  != null;
        public bool HasPowerUp => powerUpDef != null;
        public bool HasPrefab  => prefab     != null;

        public Rarity GetRarity()
        {
            if (HasWeapon)  return weaponDef.Rarity;
            if (HasPowerUp) return powerUpDef.Rarity;
            return Rarity.Common;
        }

        // Each entry is responsible for spawning its own payload.
        // To add a new payload type: add a field above and a branch here — TrySpawnLoot never needs to change.
        public bool TrySpawn(Vector3 pos, Transform parent, in SpawnPrefabs prefabs)
        {
            if (HasWeapon)
            {
                if (!prefabs.weapon) { Debug.LogWarning("[LootTable] WeaponPickup Prefab not set."); return false; }
                Instantiate(prefabs.weapon, pos, Quaternion.identity, parent).SetDefinition(weaponDef);
                return true;
            }

            if (HasPowerUp)
            {
                if (!prefabs.powerUp) { Debug.LogWarning("[LootTable] PowerUpPickup Prefab not set."); return false; }
                Instantiate(prefabs.powerUp, pos, Quaternion.identity, parent).SetDefinition(powerUpDef);
                return true;
            }

            if (upgradeItemDef)
            {
                if (!prefabs.upgrade) { Debug.LogError("[LootTable] Upgrade Pickup Prefab not assigned."); return false; }
                Instantiate(prefabs.upgrade, pos, Quaternion.identity, parent).SetDefinition(upgradeItemDef);
                return true;
            }

            if (HasPrefab)
            {
                Instantiate(prefab, pos, Quaternion.identity, parent);
                return true;
            }

            return false;
        }
    }

    [SerializeField] private List<Entry> entries = new List<Entry>();

    [Header("Overall Drop Chance")]
    [Tooltip("Multiply against entry chance (from rarity or override).")]
    [Range(0f, 1f)] public float overallDropChance = 0.5f;

    [Header("Pickup Prefabs (used when a Definition is chosen)")]
    [SerializeField] private WeaponPickup weaponPickupPrefab;
    [SerializeField] private PowerUpPickup powerUpPickupPrefab;
    [SerializeField] private WeaponUpgradePickup upgradePickupPrefab;

    [Header("Spawn")]
    [SerializeField] private Vector2 spawnJitter = new Vector2(0.25f, 0.25f);

    public bool TrySpawnLoot(Vector3 where, Transform parent = null)
    {
        if (entries == null || entries.Count == 0) return false;
        if (!Roll(overallDropChance)) return false;

        // Build weight list
        int total = 0;
        var weights = new int[entries.Count];
        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            int w = e.weightOverride > 0
                ? e.weightOverride
                : raritySettings.GetDefaultWeight(e.GetRarity());
            weights[i] = Mathf.Max(0, w);
            total += weights[i];
        }
        if (total <= 0) return false;

        // Pick one entry by weight
        var entry = entries[PickWeightedIndex(weights, total)];

        // Entry-level chance
        float chance = entry.dropChanceOverride > 0f
            ? entry.dropChanceOverride
            : raritySettings.GetDefaultDropChance(entry.GetRarity());
        if (!Roll(chance)) return false;

        Vector3 pos = where + (Vector3)new Vector2(
            UnityEngine.Random.Range(-spawnJitter.x, spawnJitter.x),
            UnityEngine.Random.Range(-spawnJitter.y, spawnJitter.y));

        return entry.TrySpawn(pos, parent, new SpawnPrefabs(weaponPickupPrefab, powerUpPickupPrefab, upgradePickupPrefab));
    }

    // Back-compat shim for older callers
    public bool TrySpawnDrop(Vector3 where) => TrySpawnLoot(where, null);

    private static bool Roll(float p) => p > 0f && UnityEngine.Random.value <= p;

    private static int PickWeightedIndex(int[] weights, int total)
    {
        int r = UnityEngine.Random.Range(0, total);
        int c = 0;
        for (int i = 0; i < weights.Length; i++)
        {
            c += weights[i];
            if (r < c) return i;
        }
        return weights.Length - 1;
    }
}
