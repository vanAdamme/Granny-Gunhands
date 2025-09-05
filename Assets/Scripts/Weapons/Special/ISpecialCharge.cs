using System;

public interface ISpecialCharge
{
    /// Raised whenever the meter changes. Arg: current damage in the pool.
    event Action<float> Changed;

    /// Accumulated player damage available to spend.
    float Current { get; }

    /// Add damage to the pool (e.g., when a player source deals damage).
    void AddDamage(float amount);

    /// Try to spend 'amount' from the pool. Returns true if successful.
    bool TryConsume(float amount);

    /// Hard reset to zero (rare: on death/scene load).
    void Reset();
}