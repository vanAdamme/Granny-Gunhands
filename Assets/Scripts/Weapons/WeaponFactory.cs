using UnityEngine;

public static class WeaponFactory
{
    /// <summary>
    /// Instantiate a weapon from its definition under 'parent'. Returns the live instance.
    /// </summary>
    public static Weapon Create(WeaponDefinition def, Transform parent)
    {
        if (!def || !def.Prefab)
        {
            Debug.LogWarning("[WeaponFactory] Missing definition or prefab.");
            return null;
        }

        // Always instantiate (avoid prefab-asset parenting issues)
        var instance = Object.Instantiate(def.Prefab, parent);
        instance.gameObject.SetActive(false); // inventory equips/activates
        instance.SetDefinition(def); // <- important for rarity/icon
        return instance;
    }
}