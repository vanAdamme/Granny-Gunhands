using UnityEngine;

[CreateAssetMenu(menuName = "Enemy/Strategy Profile", fileName = "EnemyStrategyProfile")]
public class EnemyStrategyProfile : ScriptableObject
{
    [Header("Select concrete strategy types by dropdown in the EnemyContext inspector")]
    public StrategySlot Move;
    public StrategySlot Attack;
    public StrategySlot Target;
}