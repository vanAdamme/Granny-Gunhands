using System;

public static class EnemyEvents
{
    public static event Action OnEnemyKilled;
    public static void RaiseEnemyKilled() => OnEnemyKilled?.Invoke();
}