using UnityEngine;

public abstract class InventoryItemDefinition : ItemDefinition
{
    /// Probe-only: should the Use button be enabled for this user?
    /// Default is optimistic; override in concrete items.
    public virtual bool CanUse(GameObject user) => true;

    /// Try to use/consume the item for the given user.
    public abstract bool TryUse(GameObject user);
}