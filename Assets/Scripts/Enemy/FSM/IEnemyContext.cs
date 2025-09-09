using UnityEngine;

public interface IEnemyContext
{
    Transform Transform { get; }
    float AttackRange { get; }
    float RepathInterval { get; }

    ITargetProvider TargetProvider { get; }
    IMovementStrategy Movement { get; }
    IAttackStrategy Attack { get; }

    bool IsAlive { get; }
    bool IsHurtLockedOut { get; }
    void SetHurtLock(float seconds);
    void PlayAnim(string trigger);
    void LookAt(Vector2 worldPoint);
    void OnDeath();
}