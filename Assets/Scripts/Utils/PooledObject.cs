using UnityEngine;

/// Attach to any pooled prefab. Call Release() to return to pool.
public sealed class PooledObject : MonoBehaviour
{
    private IGameObjectPool pool;

    // Called by UnityPoolService on creation
    public void Configure(IGameObjectPool p) => pool = p;

    public void Release()
    {
        if (pool != null) pool.Despawn(gameObject);
        else Destroy(gameObject); // graceful fallback if used without a pool
    }
}