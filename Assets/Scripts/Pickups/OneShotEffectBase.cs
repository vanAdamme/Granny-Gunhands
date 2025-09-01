using UnityEngine;

public abstract class OneShotEffectBase : ScriptableObject
{
    /// Return true if something meaningful happened (e.g., HP increased).
    public abstract bool ApplyOnce(IPlayerContext player);
}