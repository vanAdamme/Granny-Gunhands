using UnityEngine;

public abstract class InventoryItemDefinition : ItemDefinition
{
    /// <summary>
    /// Try to use the item with/for the given user GameObject (usually the Player root).
    /// Return true if the item was consumed.
    /// </summary>
    public abstract bool TryUse(GameObject user);
}