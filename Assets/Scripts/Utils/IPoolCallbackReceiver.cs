/// Implement this on any pooled prefab component to receive type-safe pool lifecycle callbacks
/// instead of the legacy SendMessage("OnSpawnedFromPool") / SendMessage("OnDespawnedToPool") pattern.
/// Unity 6.x deprecates SendMessage-based notification; this interface replaces it.
public interface IPoolCallbackReceiver
{
    void OnSpawnedFromPool();
    void OnDespawnedToPool();
}
