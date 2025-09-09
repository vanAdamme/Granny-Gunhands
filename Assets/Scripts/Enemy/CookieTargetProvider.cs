using UnityEngine;

public class CookieTargetProvider : MonoBehaviour, ITargetProvider
{
    [SerializeField] private Transform cookie;

    public void SetCookie(Transform t) => cookie = t;

    public bool TryGetTarget(out Transform target)
    {
        target = cookie != null ? cookie : null;
        return target != null;
    }
}