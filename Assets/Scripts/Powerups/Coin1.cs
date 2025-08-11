using UnityEngine;

public class Pentagram : MonoBehaviour
{
    [SerializeField] private ObjectGlow objectGlow;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && (objectGlow != null))
        {
            objectGlow.CallObjectGlow();
        }
    }
}