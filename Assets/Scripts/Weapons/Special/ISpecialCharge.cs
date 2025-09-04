using System;

public interface ISpecialCharge
{
    /// <summary>Raised whenever charge changes. Args: current, required.</summary>
    event Action<int,int> Changed;

    /// <summary>Current accumulated hit count.</summary>
    int Current { get; }

    /// <summary>Hits required to activate.</summary>
    int Required { get; }

    /// <summary>True when Current >= Required.</summary>
    bool IsReady { get; }

    /// <summary>Add N successful hits toward the charge.</summary>
    void AddHits(int hits);

    /// <summary>Consume charge to zero (or roll-over if you prefer different behavior).</summary>
    void Consume();

    /// <summary>Reset charge to zero without activation (e.g., on death/scene).</summary>
    void Reset();
}