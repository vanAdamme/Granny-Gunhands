using System;
using System.Collections.Generic;
using UnityEngine;

public enum Rarity { Common, Uncommon, Rare, Epic, Legendary, Mythic } // adjust to your list

[CreateAssetMenu(menuName = "Loot/Rarity Settings")]
public class RaritySettings : ScriptableObject
{
    [Serializable]
    public class Style
    {
        public Rarity rarity;
        public Color colour;
        [Range(0f,1f)] public float defaultDropChance = 1f;
        public int defaultWeight = 1;
    }

    [SerializeField] private List<Style> styles = new();

    public float GetDefaultDropChance(Rarity r)
    {
        var s = styles.Find(x => x.rarity == r);
        return s != null ? s.defaultDropChance : 1f;
    }

    public int GetDefaultWeight(Rarity r)
    {
        var s = styles.Find(x => x.rarity == r);
        return s != null ? s.defaultWeight : 1;
    }

    public Color GetColour(Rarity r)
    {
        var s = styles.Find(x => x.rarity == r);
        return s != null ? s.colour : Color.white;
    }
}