using UnityEngine;
using System;

public class SpecialChargeSimple : MonoBehaviour, ISpecialCharge
{
    [SerializeField, Min(1)] private int requiredHits = 20;
    [SerializeField] private bool allowOverfill = false;

    private int current;

    public event Action<int,int> Changed;

    public int Current => current;
    public int Required => Mathf.Max(1, requiredHits);
    public bool IsReady => current >= Required;

    public void AddHits(int hits)
    {
        if (hits <= 0) return;
        var before = current;
        current += hits;
        if (!allowOverfill && current > Required) current = Required;
        if (current != before) Changed?.Invoke(current, Required);
    }

    public void Consume()
    {
        if (current == 0) return;
        current = 0;
        Changed?.Invoke(current, Required);
    }

    public void Reset()
    {
        current = 0;
        Changed?.Invoke(current, Required);
    }

    // Optional: expose a setter for designer tweak at runtime
    public void SetRequired(int hits)
    {
        requiredHits = Mathf.Max(1, hits);
        Changed?.Invoke(current, Required);
    }
}