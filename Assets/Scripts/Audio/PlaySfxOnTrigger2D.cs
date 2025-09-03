using UnityEngine;

[DisallowMultipleComponent]
public class PlaySfxOnTrigger2D : MonoBehaviour
{
    [Header("What to play")]
    [SerializeField] private SoundEvent activateSfx;   // ScriptableObject from your audio system

    [Header("When to play")]
    [SerializeField] private LayerMask triggerLayer;   // who can trigger
    [SerializeField] private bool playOnEnter = true;  // play when something enters
    [SerializeField] private bool playOnStay  = true;  // (rate-limited) play while inside
    [SerializeField, Min(0f)] private float cooldown = 0.25f; // used by OnTriggerStay2D

    [Header("How to play")]
    [SerializeField] private bool attachToThis = true; // follow this object (positional)

    [Header("One-shot")]
    [Tooltip("If true, plays once then never again until ResetOnce() is called.")]
    [SerializeField] private bool playOnlyOnce = false;
    [Tooltip("If true and playOnlyOnce is enabled, disables this object's Collider2D after first play.")]
    [SerializeField] private bool disableColliderAfterFirstPlay = true;

    private float _nextOkTime;
    private bool  _hasPlayed;
    private Collider2D _coll;

    static bool InMask(int layer, LayerMask mask) => (mask.value & (1 << layer)) != 0;

    void Awake() => _coll = GetComponent<Collider2D>();

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!playOnEnter) return;
        if (!InMask(other.gameObject.layer, triggerLayer)) return;
        TryPlay(other.transform.position);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (!playOnStay) return;
        if (!InMask(other.gameObject.layer, triggerLayer)) return;
        TryPlay(other.transform.position);
    }

    private void TryPlay(Vector3 hitPos)
    {
        if (playOnlyOnce && _hasPlayed) return;
        if (!playOnlyOnce && Time.time < _nextOkTime) return;

        if (activateSfx)
        {
            // Mark first to avoid re-entrancy double-fires
            if (playOnlyOnce) _hasPlayed = true;

            if (attachToThis)
                AudioServicesProvider.Audio?.Play(activateSfx, attachTo: transform);
            else
                AudioServicesProvider.Audio?.Play(activateSfx, worldPos: hitPos);
        }

        if (playOnlyOnce)
        {
            if (disableColliderAfterFirstPlay && _coll) _coll.enabled = false;
            else enabled = false; // stop listening forever
        }
        else
        {
            _nextOkTime = Time.time + cooldown;
        }
    }

    /// <summary>Call this if you need to reuse the trigger later (e.g., level reset).</summary>
    public void ResetOnce()
    {
        _hasPlayed = false;
        _nextOkTime = 0f;
        if (disableColliderAfterFirstPlay && _coll) _coll.enabled = true;
        enabled = true;
    }
}