using UnityEngine;

public class DestroyOnWalls2D : MonoBehaviour
{
    [SerializeField] private LayerMask wallLayers; // set to Walls in Inspector

    private void OnTriggerEnter2D(Collider2D other)
    {
        if ((wallLayers.value & (1 << other.gameObject.layer)) != 0)
            Destroy(gameObject);
    }
}