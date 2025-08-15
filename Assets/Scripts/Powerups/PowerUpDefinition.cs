using System.Collections.Generic;
using UnityEngine;

public enum StackPolicy { RefreshDuration, StackDuration, IgnoreIfActive, ParallelInstances }
public enum VFXAttachMode { PlayerRoot, PickupOrigin, NamedAnchorOnCollector, CustomParentHint }

[CreateAssetMenu(menuName = "Powerups/PowerUp Definition")]
public class PowerUpDefinition : ScriptableObject
{
    public string displayName;
    public Sprite icon;
    [TextArea] public string description;

    [Tooltip("0 or less = permanent")]
    public float durationSeconds = 10f;
    public StackPolicy stacking = StackPolicy.RefreshDuration;

    [Header("Pickup Feedback (optional)")]
    [SerializeField] private GameObject pickupVFXPrefab;
    [SerializeField] private VFXAttachMode pickupVFXAttach = VFXAttachMode.PlayerRoot;
    [SerializeField] private string pickupAnchorName = "";     // used if NamedAnchorOnCollector
    [Tooltip("Seconds. <= 0 = auto (use animation/particle length)")]
    [SerializeField] private float pickupVFXLifetime = 0f;

    public GameObject PickupVFXPrefab => pickupVFXPrefab;
    public VFXAttachMode PickupVFXAttach => pickupVFXAttach;
    public string PickupAnchorName => pickupAnchorName;
    public float PickupVFXLifetime => pickupVFXLifetime;

    [Header("Duration VFX (optional) — follows buff lifetime")]
    [SerializeField] private GameObject durationVFXPrefab;
    [SerializeField] private VFXAttachMode durationVFXAttach = VFXAttachMode.PlayerRoot;
    [SerializeField] private string durationAnchorName = "";   // used if NamedAnchorOnCollector

    [Header("Duration VFX Options")]
    [Tooltip("If true, destroy duration VFX when the buff ends. If false, let the VFX finish naturally.")]
    [SerializeField] private bool durationVFXClampToBuff = true;

    [Tooltip("If not clamped, unparent on buff end so it stops following the player.")]
    [SerializeField] private bool durationVFXUnparentOnEnd = true;

    public GameObject DurationVFXPrefab => durationVFXPrefab;
    public VFXAttachMode DurationVFXAttach => durationVFXAttach;
    public string DurationAnchorName => durationAnchorName;
    public bool DurationVFXClampToBuff => durationVFXClampToBuff;
    public bool DurationVFXUnparentOnEnd => durationVFXUnparentOnEnd;

    // Timed/permanent effects (use IPowerUpEffect via PowerUpEffectBase)
    public List<PowerUpEffectBase> effects = new();

    // NEW: one‑shot effects (fire instantly and are NOT tracked)
    public List<OneShotEffectBase> oneShotEffects = new();
}