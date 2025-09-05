using UnityEngine;
using System;

public class SpecialChargeSimple : MonoBehaviour, ISpecialCharge
{
    [SerializeField, Min(0f)] private float current;  // damage pool

    public event Action<float> Changed;

    public float Current => current;

    public void AddDamage(float amount)
    {
        if (amount <= 0f) return;
        current += amount;
        Changed?.Invoke(current);
    }

    public bool TryConsume(float amount)
    {
        if (amount <= 0f) return true;
        if (current + 1e-5f < amount) return false;
        current -= amount;
        if (current < 0f) current = 0f;
        Changed?.Invoke(current);
        return true;
    }

    public void Reset()
    {
        current = 0f;
        Changed?.Invoke(current);
    }
}