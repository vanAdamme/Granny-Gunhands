using UnityEngine;

public class SpecialWeaponInput : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private MonoBehaviour inputServiceSource; // your InputService
    [SerializeField] private SpecialWeaponBase equippedSpecial;

    private IInputService input;

    private void Awake()
    {
        input = inputServiceSource as IInputService;
        if (input == null)
            input = Object.FindFirstObjectByType<InputService>(); // Unity 6
    }

    private void OnEnable()
    {
        if (input == null) return;
        input.SpecialStarted += OnSpecial;
    }

    private void OnDisable()
    {
        if (input == null) return;
        input.SpecialStarted -= OnSpecial;
    }

    private void OnSpecial()
    {
        if (equippedSpecial && equippedSpecial.CanActivate)
            equippedSpecial.Activate();
    }

    // Optional: runtime swapping
    public void SetEquippedSpecial(SpecialWeaponBase special) => equippedSpecial = special;
}