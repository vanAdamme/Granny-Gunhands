using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PowerUpPickup : MonoBehaviour
{
    [SerializeField] private PowerUpDefinition powerUp;

    private void Awake()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
        gameObject.tag = "Item";
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var controller = other.GetComponent<PowerUpController>();
        if (controller == null) return;

        controller.Apply(powerUp);
        Destroy(gameObject); // or return to pool
    }
}