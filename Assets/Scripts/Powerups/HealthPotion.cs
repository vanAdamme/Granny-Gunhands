using UnityEngine;

public class HealthPotion : MonoBehaviour
{
    [SerializeField] int value;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && PlayerController.Instance.IsHurt())
        {
            PlayerController.Instance.Heal(value);
            Destroy(gameObject);
        }
    }
}