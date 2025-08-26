using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class ItemInventory : MonoBehaviour
{
    [Serializable]
    public struct Stack
    {
        public ItemDefinition def;
        public int count;
        public Stack(ItemDefinition d, int c) { def = d; count = c; }
    }

    [SerializeField] private List<Stack> items = new();
    public event Action InventoryChanged;

    public IReadOnlyList<Stack> GetItems() => items;

    public void Add(ItemDefinition def, int amount = 1)
    {
        if (!def || amount <= 0) return;
        if (def.Stackable)
        {
            int idx = items.FindIndex(s => s.def == def);
            if (idx >= 0) { var s = items[idx]; s.count += amount; items[idx] = s; }
            else items.Add(new Stack(def, amount));
        }
        else
        {
            for (int i = 0; i < amount; i++) items.Add(new Stack(def, 1));
        }
            Debug.Log($"[ItemInventory {GetInstanceID()}] +{amount} {def.name}");
        InventoryChanged?.Invoke();
    }

    public bool Remove(ItemDefinition def, int amount = 1)
    {
        int idx = items.FindIndex(s => s.def == def);
        if (idx < 0) return false;

        var s = items[idx];
        if (s.count < amount) return false;

        s.count -= amount;
        if (s.count <= 0) items.RemoveAt(idx);
        else items[idx] = s;

        InventoryChanged?.Invoke();
        return true;
    }

    public bool Has(ItemDefinition def, int amount = 1)
    {
        int idx = items.FindIndex(s => s.def == def);
        return idx >= 0 && items[idx].count >= amount;
    }
}