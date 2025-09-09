using UnityEngine;

/// Implement on things that care where/which way the player (or any spawnable) was placed.
public interface IPlayerSpawnable
{
    /// Called immediately after being spawned or moved to a scene spawn point.
    /// 'position' is worldspace; 'facing' is a normalised 2D direction (can be Vector2.zero if N/A).
    void OnSpawnedAt(Vector3 position, Vector2 facing);
}
