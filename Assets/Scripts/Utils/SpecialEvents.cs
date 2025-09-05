using System;

public static class SpecialEvents
{
    /// Raised after a special weapon successfully activates.
    public static event Action Fired;

    public static void ReportFired()
    {
        Fired?.Invoke();
    }
}
