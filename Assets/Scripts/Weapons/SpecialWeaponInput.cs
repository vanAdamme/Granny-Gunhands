using UnityEngine;
using UnityEngine.InputSystem;

public class SpecialWeaponInput : MonoBehaviour
{
    [Header("Input")]
    [Tooltip("Bind this to your 'Special' action in the Input Actions asset.")]
    [SerializeField] private InputActionReference specialAction;

    [Header("Special")]
    [SerializeField] private SpecialWeaponBase equippedSpecial;

    private void OnEnable()
    {
        if (specialAction && specialAction.action != null)
            specialAction.action.performed += OnSpecialPerformed;
    }

    private void OnDisable()
    {
        if (specialAction && specialAction.action != null)
            specialAction.action.performed -= OnSpecialPerformed;
    }

    private void OnSpecialPerformed(InputAction.CallbackContext ctx)
    {
        if (equippedSpecial && equippedSpecial.CanActivate)
            equippedSpecial.Activate();
    }

    // Optional runtime swap
    public void SetEquippedSpecial(SpecialWeaponBase s) => equippedSpecial = s;
}