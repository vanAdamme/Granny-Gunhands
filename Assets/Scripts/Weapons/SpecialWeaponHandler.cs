using UnityEngine;

public class SpecialWeaponHandler : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject[] weaponPrefabs;

    private int currentWeaponIndex = 0;
    private GameObject currentWeaponInstance;
    private SpecialWeaponBehaviour currentWeaponScript;

    void Start()
    {
        EquipWeapon(0); // Equip first weapon by default
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            currentWeaponScript?.ToggleActivate();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            CycleWeapon();
        }
    }

    private void EquipWeapon(int index)
    {
        // Destroy old
        if (currentWeaponInstance != null)
            Destroy(currentWeaponInstance);

        // Instantiate new
        currentWeaponIndex = index;
        currentWeaponInstance = Instantiate(weaponPrefabs[index]);
        currentWeaponScript = currentWeaponInstance.GetComponent<SpecialWeaponBehaviour>();
    }

    private void CycleWeapon()
    {
        int nextIndex = (currentWeaponIndex + 1) % weaponPrefabs.Length;
        EquipWeapon(nextIndex);
    }
}