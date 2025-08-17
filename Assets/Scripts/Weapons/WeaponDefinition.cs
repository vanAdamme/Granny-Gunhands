using UnityEngine;

[CreateAssetMenu(menuName = "Weapons/Weapon Definition")]
public class WeaponDefinition : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string id = System.Guid.NewGuid().ToString(); // save-friendly
    [SerializeField] private string displayName;
    [SerializeField] private Sprite icon;

    [Header("Prefab")]
    [Tooltip("Prefab with a Weapon component (e.g., Pistol).")]
    [SerializeField] private Weapon weaponPrefab;
    [SerializeField] private Rarity rarity = Rarity.Common;

    public string Id          => id;
    public string DisplayName => displayName;
    public Sprite Icon        => icon;
    public Weapon Prefab      => weaponPrefab;
    public Rarity Rarity      => rarity;
}