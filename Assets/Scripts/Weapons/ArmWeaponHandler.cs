// using UnityEditor.XR;
using UnityEngine;

public enum FireButton
{
    LeftClick = 0,
    RightClick = 1
}

public class ArmWeaponHandler : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] Transform hand;
    [SerializeField] private GameObject[] weaponPrefabs;

    private int currentWeaponIndex = 0;
    private GameObject currentWeaponInstance;
    private WeaponBehaviour currentWeaponScript;

    [SerializeField] private FireButton fireButton;

    void Start()
    {
        EquipWeapon(0); // Equip first weapon by default
    }

    void Update()
    {
        AimAtMouse();

        if (Input.GetMouseButton((int)fireButton))
        {
            currentWeaponScript?.Fire();
        }

        if (Input.GetKeyDown(KeyCode.Q))
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
        currentWeaponInstance = Instantiate(weaponPrefabs[index], hand);
        currentWeaponScript = currentWeaponInstance.GetComponent<WeaponBehaviour>();
    }

    private void CycleWeapon()
    {
        int nextIndex = (currentWeaponIndex + 1) % weaponPrefabs.Length;
        EquipWeapon(nextIndex);
    }

    void AimAtMouse()
    {
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;
        Vector3 dir = mouseWorldPos - transform.position;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }
}