using System.Collections.Generic;
using UnityEngine;

public enum StackPolicy { RefreshDuration, StackDuration, IgnoreIfActive, ParallelInstances }
public enum VFXAttachMode { PlayerRoot, PickupOrigin, NamedAnchorOnCollector, CustomParentHint }

[CreateAssetMenu(menuName = "Powerups/PowerUp Definition")]
public class PowerUpDefinition : ScriptableObject
{
    [SerializeField] private string displayName;   // NEW: inspector-friendly name
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;

    [SerializeField] private Sprite icon;
    public Sprite Icon => icon;

    [TextArea] public string description;

    [SerializeField] private Rarity rarity = Rarity.Common;
    public Rarity Rarity => rarity;

    [Tooltip("0 or less = permanent")]
    public float durationSeconds = 10f;
    public StackPolicy stacking = StackPolicy.RefreshDuration;

    [Header("Pickup Feedback (optional)")]
    [SerializeField] private GameObject pickupVFXPrefab;
    [SerializeField] private VFXAttachMode pickupVFXAttach = VFXAttachMode.PlayerRoot;
    [SerializeField] private string pickupAnchorName = "";
    [Tooltip("Seconds. <= 0 = auto (use animation/particle length)")]
    [SerializeField] private float pickupVFXLifetime = 0f;

    public GameObject PickupVFXPrefab => pickupVFXPrefab;
    public VFXAttachMode PickupVFXAttach => pickupVFXAttach;
    public string PickupAnchorName => pickupAnchorName;
    public float PickupVFXLifetime => pickupVFXLifetime;

    [Header("Duration VFX (optional) — follows buff lifetime")]
    [SerializeField] private GameObject durationVFXPrefab;
    [SerializeField] private VFXAttachMode durationVFXAttach = VFXAttachMode.PlayerRoot;
    [SerializeField] private string durationAnchorName = "";

    [Header("Duration VFX Options")]
    [SerializeField] private bool durationVFXClampToBuff = true;
    [SerializeField] private bool durationVFXUnparentOnEnd = true;

    public GameObject DurationVFXPrefab => durationVFXPrefab;
    public VFXAttachMode DurationVFXAttach => durationVFXAttach;
    public string DurationAnchorName => durationAnchorName;
    public bool DurationVFXClampToBuff => durationVFXClampToBuff;
    public bool DurationVFXUnparentOnEnd => durationVFXUnparentOnEnd;

    public List<PowerUpEffectBase> effects = new();
    public List<OneShotEffectBase> oneShotEffects = new();

    // Optional: still present if something elsewhere calls it (no-op by default)
    public void Apply(IPlayerContext ctx) { /* intentionally empty; controller handles effects */ }
}