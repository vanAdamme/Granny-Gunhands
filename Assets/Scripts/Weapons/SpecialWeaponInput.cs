using UnityEngine;

public class SpecialWeaponInput : MonoBehaviour
{
    [SerializeField] private MonoBehaviour inputServiceSource; // InputService
    private IInputService input;

    [Tooltip("Any component that implements ISpecialCharge (e.g., SpecialChargeSimple)")]
    [SerializeField] private MonoBehaviour specialSource;
    private ISpecialCharge special;

    void Awake()
    {
        input = inputServiceSource as IInputService;
        if (input == null) input = FindFirstObjectByType<InputService>();

        special = specialSource as ISpecialCharge;
        if (special == null)
        {
            foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (mb is ISpecialCharge s) { special = s; break; }
            }
        }
    }

    void OnEnable()  { if (input != null) input.Special += OnSpecial; }
    void OnDisable() { if (input != null) input.Special -= OnSpecial; }

    private void OnSpecial()
    {
        special?.TryActivate();
    }
}