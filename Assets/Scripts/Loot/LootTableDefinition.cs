using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Loot/Loot Table Definition")]
public class LootTableDefinition : ScriptableObject
{
    [Header("Config")]
    [SerializeField] private RaritySettings raritySettings;

    [Serializable]
    public class Entry
    {
        [Header("Pick ONE payload")]
        public WeaponDefinition weaponDef;
        public PowerUpDefinition powerUpDef;
        public WeaponUpgradeItemDefinition upgradeItemDef;
        public GameObject prefab; // any arbitrary prefab

        [Header("Overrides (0 = use rarity defaults)")]
        [Range(0f, 1f)] public float dropChanceOverride; // per-entry chance; 0 => use rarity default
        public int weightOverride;                        // selection weight; 0 => use rarity default

        public bool HasWeapon => weaponDef != null;
        public bool HasPowerUp => powerUpDef != null;
        public bool HasPrefab => prefab != null;

        public Rarity GetRarity()
        {
            if (HasWeapon) return weaponDef.Rarity;
            if (HasPowerUp) return powerUpDef.Rarity;
            // for raw prefab entries, treat as Common by default (or add a field)
            return Rarity.Common;
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

    public void TrySpawnLoot(Vector3 where, Transform parent = null)
    {
        if (entries == null || entries.Count == 0) return;

        // 1) Global drop roll
        if (!Roll(overallDropChance)) return;

        // 2) Build weight list using rarity settings (with overrides)
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

        if (total <= 0) return;

        // 3) Pick one entry by weight
        int pick = PickWeightedIndex(weights, total);
        var entry = entries[pick];

        // 4) Entry-level chance (rarity default or override)
        float chance = entry.dropChanceOverride > 0f
            ? entry.dropChanceOverride
            : raritySettings.GetDefaultDropChance(entry.GetRarity());

        if (!Roll(chance)) return;

        // 5) Spawn the right thing
        Vector3 pos = where + (Vector3)new Vector2(UnityEngine.Random.Range(-spawnJitter.x, spawnJitter.x),
                                                   UnityEngine.Random.Range(-spawnJitter.y, spawnJitter.y));

        if (entry.HasWeapon)
        {
            if (!weaponPickupPrefab)
            {
                Debug.LogWarning("[LootTable] WeaponPickup Prefab not set.");
                return;
            }

            var pickup = Instantiate(weaponPickupPrefab, pos, Quaternion.identity, parent);
            pickup.SetDefinition(entry.weaponDef); // make sure WeaponPickup exposes SetDefinition(WeaponDefinition)
        }
        else if (entry.HasPowerUp)
        {
            if (!powerUpPickupPrefab)
            {
                Debug.LogWarning("[LootTable] PowerUpPickup Prefab not set.");
                return;
            }

            var pickup = Instantiate(powerUpPickupPrefab, pos, Quaternion.identity, parent);
            pickup.SetDefinition(entry.powerUpDef); // see PowerUpPickup below
        }
        else if (entry.upgradeItemDef)
        {
            if (!upgradePickupPrefab)
            {
                Debug.LogError("[LootTable] Upgrade Pickup Prefab not assigned.");
                return;
            }
            var p = Instantiate(upgradePickupPrefab, pos, Quaternion.identity, parent);
            p.SetDefinition(entry.upgradeItemDef);
            return;
        }
        else if (entry.HasPrefab)
        {
            Instantiate(entry.prefab, pos, Quaternion.identity, parent);
        }
        else
        {
            // nothing selected; silent no-op
        }
    }

    // Back-compat shim for older callers
    public bool TrySpawnDrop(Vector3 where)
    {
        TrySpawnLoot(where, null);
        // TrySpawnLoot is void; return true means "attempted". Change to
        // a bool return in future if you want certainty.
        return true;
    }

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