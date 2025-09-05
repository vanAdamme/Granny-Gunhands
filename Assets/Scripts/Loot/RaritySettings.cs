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

    // Editor-only guardrail to avoid spamming the console
    [System.NonSerialized] private HashSet<Rarity> warnedMissing;

    /// Full style lookup (falls back to white / 1 / 1 in editor with a warning)
    public Style Get(Rarity r)
    {
        int i = styles.FindIndex(s => s.rarity == r);
        if (i >= 0) return styles[i];

#if UNITY_EDITOR
        warnedMissing ??= new HashSet<Rarity>();
        if (!warnedMissing.Contains(r))
        {
            warnedMissing.Add(r);
            Debug.LogWarning($"[RaritySettings] No style configured for rarity '{r}'. Using defaults (white / chance=1 / weight=1).", this);
        }
#endif
        return new Style { rarity = r, colour = Color.white, defaultDropChance = 1f, defaultWeight = 1 };
    }

    /// Convenience used by LootTableDefinition
    public int GetDefaultWeight(Rarity r)
    {
        var s = Get(r);
        return Mathf.Max(0, s.defaultWeight);
    }

    /// Convenience used by LootTableDefinition
    public float GetDefaultDropChance(Rarity r)
    {
        var s = Get(r);
        return Mathf.Clamp01(s.defaultDropChance);
    }
}