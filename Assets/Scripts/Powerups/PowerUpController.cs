using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

[RequireComponent(typeof(PlayerController))]
public class PowerUpController : MonoBehaviour
{
    private IPlayerContext player;
    private readonly List<ActivePower> active = new();
    private static int nextId = 1;

    // Keep existing events if you like
    public event Action<PowerUpDefinition, float> OnPowerUpStarted;
    public event Action<PowerUpDefinition, float> OnPowerUpRefreshed;
    public event Action<PowerUpDefinition> OnPowerUpEnded;

    private void Awake()
    {
        player = GetComponent<IPlayerContext>();
    }

    private void Update()
    {
        if (active.Count == 0) return;

        float now = Time.time;

        for (int i = active.Count - 1; i >= 0; i--)
        {
            var a = active[i];

            // Expiry check for timed buffs
            if (a.expiresAt > 0f && now >= a.expiresAt)
            {
                // 1) Remove gameplay effects (reverse order for safety)
                for (int e = a.effects.Count - 1; e >= 0; e--)
                    a.effects[e].Remove(player);

                // 2) Clean up duration VFX for this instance (according to asset options)
                if (a.vfxInstance)
                {
                    if (a.def.DurationVFXClampToBuff)
                    {
                        // Hard clamp: kill the VFX exactly when the buff ends
                        Destroy(a.vfxInstance);
                    }
                    else
                    {
                        // Let it finish naturally (it will self-destroy via VFX.Destroy timer)
                        // Optional: unparent so it stops following after the buff ends
                        if (a.def.DurationVFXUnparentOnEnd)
                            a.vfxInstance.transform.SetParent(null, worldPositionStays: true);
                    }
                    a.vfxInstance = null;
                }

                // 3) Remove from active list and notify listeners
                active.RemoveAt(i);
                OnPowerUpEnded?.Invoke(a.def);

                continue;
            }

            // Keep remaining time up to date for UI
            if (a.expiresAt > 0f)
            {
                a.remaining = Mathf.Max(0f, a.expiresAt - now);
            }
        }
    }

    public void Apply(PowerUpDefinition def, Transform vfxParentHint = null, Vector3? pickupWorldOrigin = null)
    {
        float now = Time.time;
// Right before applying OneShot effects
Debug.Log($"[PowerUpController] Applying {def.name} one-shots: " +
          string.Join(", ", def.oneShotEffects.Select(e => $"{e.name} ({e.GetType().Name})")));

        // 1) One-shots fire immediately
        if (def.oneShotEffects != null)
            foreach (var one in def.oneShotEffects) one.ApplyOnce(player);

        // 2) Skip if there are no timed/permanent effects
        bool hasTimed = def.effects != null && def.effects.Count > 0;
        if (!hasTimed) return;

        // 3) Stacking behaviour (unless parallel instances)
        if (def.stacking != StackPolicy.ParallelInstances)
        {
            var existing = active.Find(p => p.def == def);
            if (existing != null)
            {
                switch (def.stacking)
                {
                    case StackPolicy.IgnoreIfActive: return;
                    case StackPolicy.RefreshDuration:
                        if (def.durationSeconds > 0) {
                            existing.expiresAt = now + def.durationSeconds;
                            existing.remaining = def.durationSeconds;
                            Replace(existing);
                            OnPowerUpRefreshed?.Invoke(def, existing.remaining);
                        }
                        return;
                    case StackPolicy.StackDuration:
                        if (def.durationSeconds > 0) {
                            existing.expiresAt += def.durationSeconds;
                            existing.remaining = existing.expiresAt - now;
                            Replace(existing);
                            OnPowerUpRefreshed?.Invoke(def, existing.remaining);
                        }
                        return;
                }
            }
        }

        // 4) Apply effects
        var effects = new List<IPowerUpEffect>(def.effects.Count);
        foreach (var e in def.effects) {
            var rt = e.CreateRuntime();
            effects.Add(rt);
            rt.Apply(player);
        }

        // 5) Spawn duration VFX (lives until buff end; controller cleans it up)
        var collector = (player as MonoBehaviour)?.transform;
        var parent    = ResolveAttachTransform(def, collector, vfxParentHint);
        Vector3 origin = pickupWorldOrigin ?? (collector ? collector.position : Vector3.zero);

        GameObject durationVFX = null;
        if (def.DurationVFXPrefab)
        {
            // autoDestroy:false → don’t time out on clip length; end with the buff.
            durationVFX = VFX.SpawnAttached(def.DurationVFXPrefab, parent, origin, 1.5f, autoDestroy: false);
        }

        // 6) Track instance
        var instance = new ActivePower {
            id         = nextId++,
            def        = def,
            effects    = effects,
            expiresAt  = def.durationSeconds > 0 ? now + def.durationSeconds : -1f,
            remaining  = def.durationSeconds > 0 ? def.durationSeconds : -1f,
            vfxInstance = durationVFX
        };

        active.Add(instance);
        OnPowerUpStarted?.Invoke(def, instance.remaining);
    }

    // UI-friendly snapshot (no allocation if reusing the buffer)
    public void BuildSnapshot(List<ActiveInfo> buffer)
    {
        buffer.Clear();
        foreach (var a in active)
            buffer.Add(new ActiveInfo(a.id, a.def, a.remaining));
    }

    public readonly struct ActiveInfo
    {
        public readonly int id;
        public readonly PowerUpDefinition definition;
        public readonly float remaining;
        public ActiveInfo(int id, PowerUpDefinition def, float remaining)
        { this.id = id; this.definition = def; this.remaining = remaining; }
    }

    private void Replace(ActivePower updated)
    {
        int idx = active.FindIndex(x => x.id == updated.id);
        if (idx >= 0) active[idx] = updated;
    }

    private sealed class ActivePower
    {
        public int id;
        public PowerUpDefinition def;
        public List<IPowerUpEffect> effects;
        public float expiresAt;
        public float remaining;
        public GameObject vfxInstance;
    }

    private static Transform ResolveAttachTransform(PowerUpDefinition def, Transform collector, Transform customHint)
    {
        switch (def.DurationVFXAttach)
        {
            case VFXAttachMode.PlayerRoot:
                return collector;
            case VFXAttachMode.PickupOrigin:
                return null; // spawn in world at pickup spot (handled by caller)
            case VFXAttachMode.NamedAnchorOnCollector:
                var anchors = collector ? collector.GetComponent<VFXAttachPoints>() : null;
                var t = anchors ? anchors.Get(def.DurationAnchorName) : null;
                return t ? t : collector; // fallback to collector root if missing
            case VFXAttachMode.CustomParentHint:
                return customHint ? customHint : collector;
            default:
                return collector;
        }
    }

    private static GameObject SpawnAttached(GameObject prefab, Transform parent, Vector3 worldPos, float fallbackLifetime)
    {
        if (!prefab) return null;

        // If we have a parent, spawn as a child at parent position; otherwise at worldPos (static)
        var go = parent
            ? Instantiate(prefab, parent.position, Quaternion.identity, parent)
            : Instantiate(prefab, worldPos, Quaternion.identity);

        // Safety lifetime if the prefab doesn't self‑destroy
        float lifetime = fallbackLifetime;
        var ps = go.GetComponentInChildren<ParticleSystem>();
        if (ps != null)
        {
            var main = ps.main;
            lifetime = Mathf.Max(lifetime, main.duration + main.startLifetime.constantMax);
        }

        // If we attach for duration, we usually DON'T auto‑destroy here. The end event will clean up.
        // For static spawn (no parent), we can still let the end event clean it up; no harm if it finishes earlier.
        return go;
    }
}