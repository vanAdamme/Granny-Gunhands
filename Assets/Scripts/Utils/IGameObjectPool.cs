using UnityEngine;

public interface IGameObjectPool
{
    GameObject Spawn(GameObject prefab, Vector3 pos, Quaternion rot, Transform parent = null);
    void Despawn(GameObject instance);
    void Prewarm(GameObject prefab, int count);
}