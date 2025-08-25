using UnityEngine;

[CreateAssetMenu(menuName = "Environment/Breakable Definition")]
public class BreakableDefinition : ScriptableObject
{
    [Min(1)] public float maxHealth = 10f;

    [System.Serializable]
    public struct Stage
    {
        [Tooltip("When current/max <= threshold, this sprite becomes active.")]
        [Range(0f,1f)] public float threshold;
        public Sprite sprite;
    }

    [Header("Damage Stages (sort high→low recommended)")]
    public Stage[] stages;

    [Header("Feedback (optional)")]
    public GameObject vfxOnHit;
    public float vfxOnHitLifetime = 0.5f;
    public GameObject vfxOnDeath;
    public float vfxOnDeathLifetime = 1.2f;
    public AudioClip sfxOnHit;
    public AudioClip sfxOnDeath;
}