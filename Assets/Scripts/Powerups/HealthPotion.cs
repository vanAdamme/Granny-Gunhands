using UnityEngine;

public class HealthPotion : MonoBehaviour
{
    [SerializeField] int value;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && PlayerController.Instance.isHurt())
        {
            PlayerController.Instance.Heal(value);
            Destroy(gameObject);
        }
    }
}