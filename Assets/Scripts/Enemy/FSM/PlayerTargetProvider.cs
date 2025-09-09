using UnityEngine;

public class PlayerTargetProvider : MonoBehaviour, ITargetProvider
{
    [SerializeField] private Transform player;

    void Reset()
    {
        if (!player)
        {
            var p = Object.FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);
            if (p) player = p.transform;
        }
    }

    public bool TryGetTarget(out Transform t)
    {
        t = player;
        return t != null;
    }
}