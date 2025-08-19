using System.Collections.Generic;
using UnityEngine;

public class EnemySpawnPoint : MonoBehaviour
{
    [SerializeField] private Health enemyPrefab; // your enemy root has Health
    [SerializeField, Min(1)] private int count = 1;
    [SerializeField] private float radius = 0.5f;

    public IEnumerable<Health> Spawn()
    {
        var spawned = new List<Health>(count);
        if (!enemyPrefab) yield break;

        for (int i = 0; i < count; i++)
        {
            Vector2 offset = Random.insideUnitCircle * radius;
            var h = Instantiate(enemyPrefab, transform.position + (Vector3)offset, Quaternion.identity);
            spawned.Add(h);
            yield return h;
        }
    }
}