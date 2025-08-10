using System.Collections.Generic;
using UnityEngine;

public enum StackPolicy { RefreshDuration, StackDuration, IgnoreIfActive, ParallelInstances }

[CreateAssetMenu(menuName = "Powerups/PowerUp Definition")]
public class PowerUpDefinition : ScriptableObject
{
    public string displayName;
    public Sprite icon;
    [TextArea] public string description;

    [Tooltip("0 or less = permanent")]
    public float durationSeconds = 10f;

    public StackPolicy stacking = StackPolicy.RefreshDuration;

    // Drag concrete effect assets here (see examples below)
    public List<PowerUpEffectBase> effects;
}