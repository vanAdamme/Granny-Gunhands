using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// Minimal, decoupled charge meter for “kill to charge → press Special to activate”.
public class SpecialChargeSimple : MonoBehaviour, ISpecialCharge
{
    [Header("Charge")]
    [Min(1)] public int killsRequired = 5;
    [Min(0)] public int currentKills = 0;
    [Tooltip("Allow multiple activations to bank up.")]
    public bool allowStacks = false;
    [Min(1)] public int maxStacks = 3;
    private int stackedCharges = 0;

    [Header("Ability hook")]
    [Tooltip("Optional: anything implementing ISpecialAbility; Activate() will be called on use.")]
    [SerializeField] private MonoBehaviour abilitySource;
    private ISpecialAbility ability;

    [Header("UI (optional)")]
    [SerializeField] private Slider slider;          // 0..1 progress
    [SerializeField] private Image fillImage;        // alternative fill
    [SerializeField] private UnityEvent onActivated; // SFX/VFX hook

    void Awake()
    {
        ability = abilitySource as ISpecialAbility;
        UpdateUI();
    }

    public void AddCharge(int amount)
    {
        if (amount <= 0) return;

        currentKills += amount;
        while (currentKills >= killsRequired)
        {
            currentKills -= killsRequired;
            stackedCharges++;

            if (!allowStacks || stackedCharges >= maxStacks)
            {
                // Clamp & carry remainder appropriately
                stackedCharges = Mathf.Clamp(stackedCharges, 0, maxStacks);
                if (!allowStacks) currentKills = Mathf.Min(currentKills, killsRequired - 1);
                break;
            }
        }
        UpdateUI();
    }

    public bool TryActivate()
    {
        if (stackedCharges > 0)
        {
            stackedCharges--;
            onActivated?.Invoke();
            bool ok = ability?.Activate() ?? true; // If no ability wired, still consume charge
            UpdateUI();
            return ok;
        }

        if (currentKills >= killsRequired)
        {
            currentKills -= killsRequired;
            onActivated?.Invoke();
            bool ok = ability?.Activate() ?? true;
            UpdateUI();
            return ok;
        }

        return false; // not enough charge
    }

    private void UpdateUI()
    {
        float fill = killsRequired > 0 ? Mathf.Clamp01(currentKills / (float)killsRequired) : 0f;
        if (slider) slider.value = fill;
        if (fillImage) fillImage.fillAmount = fill;
        // You can add a text badge for stackedCharges if you like.
    }

    // Convenience for enemies: call this on kill.
    public void NotifyKill() => AddCharge(1);
}