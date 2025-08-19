using System.Collections.Generic;
using UnityEngine;

public enum WeaponCategory { Pistol, Shotgun, Laser, SMG, Rifle, Rocket, Other }

[CreateAssetMenu(menuName = "Weapons/Weapon Definition")]
public class WeaponDefinition : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string id = System.Guid.NewGuid().ToString();
    [SerializeField] private string displayName;
    [SerializeField] private Sprite icon;
    [SerializeField] private Rarity rarity = Rarity.Common;

    [Header("Classification")]
    [SerializeField] private WeaponCategory category = WeaponCategory.Other;
    public WeaponCategory Category => category;

    [Header("Prefab (factory uses this)")]
    [SerializeField] private Weapon prefab;

    [Header("Levels")]
    [SerializeField] private List<WeaponLevelData> levels = new();

    public string Id => id;
    public string DisplayName => displayName;
    public Sprite Icon => icon;
    public Rarity Rarity => rarity;
    public Weapon Prefab => prefab;
    public IReadOnlyList<WeaponLevelData> Levels => levels;

    public WeaponLevelData GetLevelData(int index)
    {
        if (levels == null || levels.Count == 0) return null;
        index = Mathf.Clamp(index, 0, levels.Count - 1);
        return levels[index];
    }

    [System.Serializable]
    public class WeaponLevelData
    {
        [Min(1)] public int level = 1;
        [Header("Firing")]
        [Min(0.01f)] public float cooldown = 0.15f;
        [Min(0.1f)]  public float damage = 3f;
        [Min(0.1f)]  public float range = 12f;
        [Min(0.1f)]  public float projectileSpeed = 18f;

        [Header("Behaviour")]
        public LayerMask targetLayers;
        public LayerMask obstacleLayers;
        public int  maxPierces = 0;
        public bool pierceThroughObstacles = false;

        [Header("Visuals")]
        public Sprite spriteOverride;
        public GameObject muzzleFlashPrefab;

        [Header("Projectile Prefabs")]
        public GameObject projectilePrefab;
        public GameObject grenadePrefab;
    }
}