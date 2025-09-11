using UnityEngine;

public sealed class NullAttackStrategy : MonoBehaviour, IAttackStrategy
{
    public void OnEnter(IEnemyContext _) {}
    public bool TryAttack(IEnemyContext _, Transform __, float ___) => false;
    public void OnExit(IEnemyContext  _) {}
}