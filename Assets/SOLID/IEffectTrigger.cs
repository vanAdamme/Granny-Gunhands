using UnityEngine;

/// <summary>
/// Defines a contract for triggering effects, such as particle systems or sound effects, at a specific location.
/// </summary>
public interface IEffectTrigger
{
    void TriggerEffect(Vector2 position);
}