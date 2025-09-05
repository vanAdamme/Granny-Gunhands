using UnityEngine;
using UnityEngine.InputSystem;

public class SpecialWeaponInput : MonoBehaviour
{
    [Header("Input (either/or)")]
    [Tooltip("Optional. If set, we'll listen to this InputAction directly.")]
    [SerializeField] private InputActionReference specialAction;

    [Tooltip("Optional. If no InputAction is set, we'll listen to this IInputService (or the singleton).")]
    [SerializeField] private MonoBehaviour inputServiceSource; // implements IInputService

    [Header("Special")]
    [SerializeField] private SpecialWeaponBase equippedSpecial;
    public SpecialWeaponBase EquippedSpecial => equippedSpecial;
    
    [SerializeField] private bool logDebug = true;

    private bool weEnabledAction;
    private IInputService inputService;
    private InputAction action;

    private void Awake()
    {
        // Be helpful if someone forgets to assign the special
        if (!equippedSpecial)
            equippedSpecial = GetComponentInParent<SpecialWeaponBase>();
    }

    private void OnEnable()
    {
        // Prefer direct InputAction if assigned
        action = specialAction ? specialAction.action : null;
        if (action != null)
        {
            if (!action.enabled) { action.Enable(); weEnabledAction = true; if (logDebug) Debug.Log("[SpecialWeaponInput] Enabled Special action."); }
            action.performed += OnActionPerformed;
            return;
        }

        // Fallback: use IInputService.Special event
        inputService = (inputServiceSource as IInputService)
                       ?? InputService.Instance
                       ?? Object.FindFirstObjectByType<InputService>(FindObjectsInactive.Include);

        if (inputService != null)
        {
            inputService.Special += OnServiceSpecial;
            if (logDebug) Debug.Log("[SpecialWeaponInput] Hooked to IInputService.Special.");
        }
        else if (logDebug)
        {
            Debug.LogWarning("[SpecialWeaponInput] No Special action and no IInputService found.");
        }
    }

    private void OnDisable()
    {
        if (action != null)
        {
            action.performed -= OnActionPerformed;
            if (weEnabledAction && action.enabled) action.Disable();
            weEnabledAction = false;
            action = null;
        }

        if (inputService != null)
        {
            inputService.Special -= OnServiceSpecial;
            inputService = null;
        }
    }

    private void OnActionPerformed(InputAction.CallbackContext ctx) => TryActivateSpecial();
    private void OnServiceSpecial() => TryActivateSpecial();

    private void TryActivateSpecial()
    {
        if (!equippedSpecial)
        {
            if (logDebug) Debug.LogWarning("[SpecialWeaponInput] No equippedSpecial.");
            return;
        }

        if (!equippedSpecial.CanActivate)
        {
            if (logDebug) Debug.Log("[SpecialWeaponInput] Pressed but not ready.");
            return;
        }

        equippedSpecial.Activate(); // consumes from the shared damage pool & fires event
        if (logDebug) Debug.Log("[SpecialWeaponInput] Special ACTIVATED.");
    }

    public void SetEquippedSpecial(SpecialWeaponBase s) => equippedSpecial = s;
}