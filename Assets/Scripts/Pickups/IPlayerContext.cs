using UnityEngine;

// IPlayerContext composes the four focused sub-interfaces.
// New code should depend on the narrowest interface that satisfies its needs
// (e.g. a heal effect only needs IHealthContext; a speed buff only needs IMovementContext).
public interface IPlayerContext : IHealthContext, IMovementContext, IProgressionContext, IWeaponContext
{
    Transform Transform { get; }
    ItemInventory ItemInventory { get; }
}