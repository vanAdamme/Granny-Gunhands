using UnityEngine;

public class CookieBribeSpecial : SpecialWeaponBase
{
    [Header("Spawn")]
    [SerializeField] private CookieBribeDecoy decoyPrefab;
    [SerializeField] private float decoyLifetime = 6f;

    [Header("Effect")]
    [SerializeField] private float bribeDuration = 6f;
    [SerializeField] private float bribeRadius = 10f;
    [Tooltip("Enemies must be on this Layer (or adjust as needed).")]
    [SerializeField] private LayerMask enemyMask = -1;

    /// <summary>
    /// Base class calls this after spending charge. We keep it arg-less so
    /// SpecialWeaponInput and the rest of your system remain untouched.
    /// </summary>
    protected override void ActivateInternal()
    {
        // Spawn at the player if present; otherwise at this object.
        Vector2 spawnPos;
        var player = GetComponentInParent<PlayerController>();
        if (player != null) spawnPos = player.transform.position;
        else                spawnPos = transform.position;

        Activate(spawnPos);
    }

    /// <summary>
    /// Optional helper: allows other systems/tests to trigger the cookie
    /// at an explicit world position without touching the input code.
    /// </summary>
    public bool Activate(Vector2 worldPos)
    {
        if (!decoyPrefab)
        {
            Debug.LogWarning("[CookieBribeSpecial] No decoy prefab assigned.");
            return false;
        }

        var decoy = Instantiate(decoyPrefab, worldPos, Quaternion.identity);
        decoy.SetLifetime(decoyLifetime);

        BribeNearbyEnemies(decoy.transform);
        return true;
    }

    private void BribeNearbyEnemies(Transform cookie)
    {
        // Collider-free, layer-free scan: look for all active AIPath agents, then filter by distance.
        var aiAgents = FindObjectsByType<Pathfinding.AIPath>(FindObjectsSortMode.None);
        int count = 0;

        // Square the radius to avoid a bunch of sqrts in a loop
        float r2 = bribeRadius * bribeRadius;
        Vector3 cpos = cookie.position;

        for (int i = 0; i < aiAgents.Length; i++)
        {
            var ai = aiAgents[i];
            if (!ai || !ai.isActiveAndEnabled) continue;

            // Distance check
            var d2 = (ai.transform.position - cpos).sqrMagnitude;
            if (d2 > r2) continue;

            // Host = the transform that actually holds AIPath/IAstarAI
            var host = ai.transform;
            var bribed = host.GetComponent<BribedAI>();
            if (!bribed) bribed = host.gameObject.AddComponent<BribedAI>();

            bribed.ApplyBribe(cookie, bribeDuration);
            count++;

#if UNITY_EDITOR
    Debug.Log($"[CookieBribeSpecial] Applied bribe to '{host.name}' via AIPath (dist^2={d2:F2})");
#endif
        }

#if UNITY_EDITOR
    Debug.Log($"[CookieBribeSpecial] Bribed {count} enemies within {bribeRadius} units (AIPath scan).");
#endif
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, bribeRadius);
    }
#endif
}