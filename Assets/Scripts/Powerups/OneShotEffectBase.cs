using UnityEngine;

public abstract class OneShotEffectBase : ScriptableObject
{
    public abstract void ApplyOnce(IPlayerContext player);
}