using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class SpecialChargeMeter : MonoBehaviour
{
    [SerializeField, Min(1f)] private int killsRequired = 10;

    public int Current { get; private set; }
    public int Required => killsRequired;
    public bool IsFull => Current >= Required;

    void OnEnable()
    {
        EnemyEvents.OnEnemyKilled += HandleKill;
        PushUI();
    }

    void OnDisable()
    {
        EnemyEvents.OnEnemyKilled -= HandleKill;
    }

    void HandleKill()
    {
        Current = Mathf.Min(Required, Current + 1);
        PushUI();
    }

    void PushUI()
    {
        UIController.Instance.UpdateKillCharge(Current, Required);
    }

    public bool SpendFull()
    {
        if (!IsFull) return false;
        Current = 0;
        PushUI();
        return true;
    }
}