using UnityEngine;

public abstract class PowerUpEffectBase : ScriptableObject
{
    public abstract IPowerUpEffect CreateRuntime();
}