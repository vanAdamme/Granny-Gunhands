using UnityEngine;

public static class VFX
{
    /// <summary>
    /// Spawn a VFX prefab at a world position (not parented).
    /// If fallbackLifetime <= 0, use animation/particle length (auto).
    /// Otherwise destroy after max(anim/particle, fallbackLifetime).
    /// </summary>
    public static GameObject Spawn(
        GameObject prefab,
        Vector3 position,
        Quaternion rotation,
        float fallbackLifetime = 1.5f,
        bool autoDestroy = true)
    {
        if (!prefab) return null;

        var go = Object.Instantiate(prefab, position, rotation);

        float life = ComputeLifetime(go, fallbackLifetime);
        if (autoDestroy) Object.Destroy(go, life);

        return go;
    }

    /// <summary>
    /// Spawn a VFX prefab parented to 'parent' (if provided) or at worldPos.
    /// If fallbackLifetime <= 0, use animation/particle length (auto).
    /// Otherwise destroy after max(anim/particle, fallbackLifetime).
    /// </summary>
    public static GameObject SpawnAttached(
        GameObject prefab,
        Transform parent,
        Vector3 worldPos,
        float fallbackLifetime = 1.5f,
        bool autoDestroy = true)
    {
        if (!prefab) return null;

        var go = parent
            ? Object.Instantiate(prefab, parent.position, Quaternion.identity, parent)
            : Object.Instantiate(prefab, worldPos, Quaternion.identity);

        float life = ComputeLifetime(go, fallbackLifetime);
        if (autoDestroy) Object.Destroy(go, life);

        return go;
    }

    /// <summary>
    /// If fallbackLifetime <= 0 → return the longest Animator/Particle duration
    ///   (or a tiny safe default if none found).
    /// If fallbackLifetime  > 0 → return max(computed, fallbackLifetime).
    /// </summary>
    private static float ComputeLifetime(GameObject root, float fallbackLifetime)
    {
        float computed = 0f;

        // Particles: take the maximum across all systems
        var particles = root.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particles.Length; i++)
        {
            var main = particles[i].main;
            float life = main.duration;

            var sl = main.startLifetime;
            life += sl.mode switch
            {
                ParticleSystemCurveMode.TwoConstants => sl.constantMax,
                ParticleSystemCurveMode.TwoCurves   => sl.constantMax,
                _                                   => sl.constant
            };

            if (life > computed) computed = life;
        }

        // Animators: take the longest clip length on any controller
        var animators = root.GetComponentsInChildren<Animator>(true);
        for (int i = 0; i < animators.Length; i++)
        {
            var ctrl = animators[i].runtimeAnimatorController;
            if (!ctrl) continue;

            var clips = ctrl.animationClips;
            for (int c = 0; c < clips.Length; c++)
            {
                var clip = clips[c];
                if (clip && clip.length > computed) computed = clip.length;
            }
        }

        if (fallbackLifetime <= 0f)
            return Mathf.Max(0.25f, computed); // auto mode; tiny floor if nothing found

        return Mathf.Max(fallbackLifetime, computed);
    }
}