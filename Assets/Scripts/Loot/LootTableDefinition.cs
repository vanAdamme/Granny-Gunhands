using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Loot/Loot Table")]
public class LootTableDefinition : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        [Header("Pick ONE payload")]
        public WeaponDefinition weaponDef;
        public PowerUpDefinition powerUpDef;
        public GameObject prefab;

        [Header("Overrides (0 = use rarity defaults)")]
        [Range(0f,1f)] public float dropChanceOverride;
        [Min(0)] public int weightOverride;
    }

    [Header("Config")]
    [SerializeField] private RaritySettings raritySettings;
    [SerializeField] private List<Entry> entries = new();
    [SerializeField, Range(0f,1f)] private float overallDropChance = 1f; // roll to drop anything at all
    [SerializeField] private WeaponPickup weaponPickupPrefab;
    [SerializeField] private PowerUpPickup powerUpPickupPrefab;
    [SerializeField] private Vector2 spawnJitter = new(0.25f, 0.25f);

    public bool TrySpawnDrop(Vector3 worldPos)
    {
        if (Random.value > overallDropChance) return false;

        var candidates = new List<(Entry e, float weight)>();
        float total = 0f;

        foreach (var e in entries)
        {
            if (e == null) continue;

            float chance, weight;

            if (e.weaponDef)
            {
                var st = raritySettings ? raritySettings.Get(e.weaponDef.Rarity) : default;
                chance = e.dropChanceOverride > 0f ? e.dropChanceOverride : st.defaultDropChance;
                weight = e.weightOverride > 0 ? e.weightOverride : Mathf.Max(1, st.defaultWeight);
            }
            else
            {
                // Non-weapon entries: honour manual overrides only
                chance = e.dropChanceOverride;
                weight = Mathf.Max(1, e.weightOverride);
            }

            if (chance <= 0f || weight <= 0f) continue;
            if (Random.value > chance) continue;

            candidates.Add((e, weight));
            total += weight;
        }

        if (candidates.Count == 0) return false;

        float r = Random.value * total;
        foreach (var c in candidates)
        {
            if ((r -= c.weight) > 0f) continue;
            Spawn(c.e, worldPos + new Vector3(
                Random.Range(-spawnJitter.x, spawnJitter.x),
                Random.Range(-spawnJitter.y, spawnJitter.y), 0));
            return true;
        }
        return false;
    }

    private void Spawn(Entry e, Vector3 p)
    {
        if (e.weaponDef && weaponPickupPrefab)
        {
            var pick = Instantiate(weaponPickupPrefab, p, Quaternion.identity);
            pick.Init(e.weaponDef);
            return;
        }
        if (e.powerUpDef && powerUpPickupPrefab)
        {
            var pick = Instantiate(powerUpPickupPrefab, p, Quaternion.identity);
            pick.Init(e.powerUpDef);
            return; 
        }
        if (e.prefab) Instantiate(e.prefab, p, Quaternion.identity);
    }

    #if UNITY_EDITOR
    void OnValidate()
    {
        foreach (var e in entries)
        {
            if (e == null) continue;
            int count = (e.weaponDef ? 1 : 0) + (e.powerUpDef ? 1 : 0) + (e.prefab ? 1 : 0);
            if (count > 1)
                Debug.LogWarning($"[LootTable] Entry has multiple payloads; only one will be used.", this);
            if (!weaponPickupPrefab && e.weaponDef)
                Debug.LogWarning($"[LootTable] Weapon entry but Weapon Pickup Prefab is not assigned.", this);
            if (!powerUpPickupPrefab && e.powerUpDef)
                Debug.LogWarning($"[LootTable] PowerUp entry but PowerUp Pickup Prefab is not assigned.", this);
        }
    }
    #endif
}