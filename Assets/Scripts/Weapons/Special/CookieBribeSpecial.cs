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
        // Prefer the actual player singleton; do NOT trust our own transform.
        var player = PlayerController.Instance 
                    ?? FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);

        Vector3 spawnPos = player ? player.transform.position : transform.position;

        // Optional: small forward offset so the cookie isn’t inside the player collider
        // (comment out if you don’t want it)
        spawnPos += new Vector3(0.25f, 0f, 0f);

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

        return true;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, bribeRadius);
    }
#endif
}