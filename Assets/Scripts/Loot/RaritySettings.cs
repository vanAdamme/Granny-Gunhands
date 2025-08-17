using System.Collections.Generic;
using UnityEngine;

public enum Rarity { Common, Uncommon, Rare, Epic, Legendary, Mythic }

[CreateAssetMenu(menuName = "Loot/Rarity Settings")]
public class RaritySettings : ScriptableObject
{
    [System.Serializable]
    public struct Style
    {
        public Rarity rarity;
        public Color  colour;              // UI tint
        [Range(0f,1f)] public float defaultDropChance;
        [Min(0)] public int defaultWeight; // relative weight among qualifiers
    }

    [SerializeField] private List<Style> styles = new();

    public Style Get(Rarity r)
    {
        int i = styles.FindIndex(s => s.rarity == r);
        return (i >= 0) ? styles[i] : new Style { rarity=r, colour=Color.white, defaultDropChance=1f, defaultWeight=1 };
    }
}