using UnityEngine;

public class SpecialWeaponInput : MonoBehaviour
{
    [SerializeField] private SpecialWeaponBase equippedSpecial;
    [SerializeField] private KeyCode activationKey = KeyCode.Space;

    void Update()
    {
        if (Time.timeScale == 0f || !equippedSpecial) return;

        if (Input.GetKeyDown(activationKey))
        {
            equippedSpecial.TryActivate();
        }
    }

    public void Equip(SpecialWeaponBase special) => equippedSpecial = special;
}