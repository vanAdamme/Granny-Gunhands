using UnityEngine;

public interface ITargetProvider
{
    bool TryGetTarget(out Transform target); // player or cookie, etc.
}

public interface IMovementStrategy
{
    // Move toward a destination; return true if moving, false if already close
    bool MoveTowards(IEnemyContext ctx, Vector2 destination, float dt);
}

public interface IAttackStrategy
{
    // Return true if we attacked this tick (and may need a cooldown)
    bool TryAttack(IEnemyContext ctx, Transform target, float dt);
}