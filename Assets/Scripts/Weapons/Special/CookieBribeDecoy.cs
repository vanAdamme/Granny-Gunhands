using UnityEngine;

public class CookieBribeDecoy : MonoBehaviour
{
    [SerializeField] private float lifetime = 6f;

    private void OnEnable()
    {
        if (lifetime > 0f)
            Destroy(gameObject, lifetime);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Handy when you want to see where the cookie spawned
        Gizmos.DrawIcon(transform.position, "d_Favorite@2x", true);
    }
#endif

    public void SetLifetime(float seconds) => lifetime = seconds;
}