using System.Collections.Generic;
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
            if (a.expiresAt > 0 && now >= a.expiresAt)
            {
                for (int e = a.effects.Count - 1; e >= 0; e--)
                    a.effects[e].Remove(player);

                active.RemoveAt(i);
                OnPowerUpEnded?.Invoke(a.def);
            }
            else if (a.expiresAt > 0)
            {
                a.remaining = a.expiresAt - now;
                active[i] = a; // struct copy safety if I switch to struct later
            }
        }
    }

    public void Apply(PowerUpDefinition def)
    {
        float now = Time.time;

        // If parallel, always create a new instance
        if (def.stacking != StackPolicy.ParallelInstances)
        {
            var existing = active.Find(p => p.def == def);
            if (existing != null)
            {
                switch (def.stacking)
                {
                    case StackPolicy.IgnoreIfActive:
                        return;
                    case StackPolicy.RefreshDuration:
                        if (def.durationSeconds > 0)
                        {
                            existing.expiresAt = now + def.durationSeconds;
                            existing.remaining = def.durationSeconds;
                            Replace(existing);
                            OnPowerUpRefreshed?.Invoke(def, existing.remaining);
                        }
                        return;
                    case StackPolicy.StackDuration:
                        if (def.durationSeconds > 0)
                        {
                            existing.expiresAt += def.durationSeconds;
                            existing.remaining = existing.expiresAt - now;
                            Replace(existing);
                            OnPowerUpRefreshed?.Invoke(def, existing.remaining);
                        }
                        return;
                }
            }
        }

        // Create a fresh runtime instance
        var effects = new List<IPowerUpEffect>(def.effects.Count);
        foreach (var e in def.effects)
        {
            var rt = e.CreateRuntime();
            effects.Add(rt);
            rt.Apply(player);
        }

        var instance = new ActivePower
        {
            id = nextId++,
            def = def,
            effects = effects,
            expiresAt = def.durationSeconds > 0 ? now + def.durationSeconds : -1f,
            remaining = def.durationSeconds > 0 ? def.durationSeconds : -1f
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
    }
}