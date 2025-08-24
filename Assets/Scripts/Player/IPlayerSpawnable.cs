using UnityEngine;

public interface IPlayerSpawnable
{
    void OnSpawnedAt(Vector3 position, Vector2 facing);
}