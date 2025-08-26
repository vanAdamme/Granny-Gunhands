using UnityEngine;

public abstract class InventoryItemDefinition : ItemDefinition {
    public virtual bool CanUse(GameObject user) => true; // default optimistic
    public abstract bool TryUse(GameObject user);
}