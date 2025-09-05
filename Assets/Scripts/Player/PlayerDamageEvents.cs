using System;

public static class PlayerDamageEvents
{
    /// <summary>Raised whenever the PLAYER deals damage to an enemy. Units: hitpoints.</summary>
    public static event Action<float> DamagedEnemy;

    public static void Report(float amount)
    {
        if (amount <= 0f) return;
        DamagedEnemy?.Invoke(amount);
    }
}