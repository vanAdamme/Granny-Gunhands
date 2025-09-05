using System;

public static class SpecialEvents
{
    /// <summary>Raised after a special weapon successfully activates. Arg: cost spent.</summary>
    public static event Action<float> Fired;

    public static void ReportFired(float cost)
    {
        Fired?.Invoke(cost);
    }
}