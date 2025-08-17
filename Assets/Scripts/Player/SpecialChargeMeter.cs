using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class SpecialChargeMeter : MonoBehaviour
{
    [SerializeField, Min(1f)] private int killsRequired = 10;

    public int Current { get; private set; }
    public int Required => Mathf.Max(1, killsRequired);
    public bool IsFull => Current >= Required;

    bool pendingUiSync;

    void OnEnable()
    {
        EnemyEvents.OnEnemyKilled += HandleKill;
        PushUI();
    }

    void OnDisable()
    {
        EnemyEvents.OnEnemyKilled -= HandleKill;
    }

    void Update()
    {
        // Late bind: if UI wasn't ready earlier, try again when it exists
        if (pendingUiSync && UIController.Instance != null)
        {
            pendingUiSync = false;
            PushUI();
        }

        if (Current > 0)
        {
            // decay as float but clamp to int for display; optional
            float f = Mathf.Max(0f, Current * Time.deltaTime);
            int newVal = Mathf.FloorToInt(f);
            if (newVal != Current) { Current = newVal; PushUI(); }
        }
    }

    void HandleKill()
    {
        if (IsFull) return;
        Current = Mathf.Min(Required, Current + 1);
        PushUI();
    }

    void PushUI()
    {
        var ui = UIController.Instance;
        if (ui == null) { pendingUiSync = true; return; }

        // If you added a dedicated kill-charge slider:
        ui.UpdateKillCharge(Current, Required);
    }

    public bool SpendFull()
    {
        if (!IsFull) return false;
        Current = 0;
        PushUI();
        return true;
    }
}