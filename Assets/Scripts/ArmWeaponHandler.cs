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
    // [SerializeField] GameObject muzzle;
    [SerializeField] Transform hand;
    // [SerializeField] GameObject projectile;
    // [SerializeField] private Transform bulletSpawnPoint;
    [SerializeField] private GameObject[] weaponPrefabs;


    private int currentWeaponIndex = 0;
    private GameObject currentWeaponInstance;
    private WeaponBehaviour currentWeaponScript;

    [SerializeField] private FireButton fireButton;

    Transform closestEnemy;
    // [SerializeField] GameObject grandChild;
    // Animator anim;

    void Start()
    {
        EquipWeapon(0); // Equip first weapon by default
        // anim = GetComponent<Animator>();
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
   
        // Vector3 mousePosScreen = Input.mousePosition;
        // Vector3 mousePosWorld = Camera.main.ScreenToWorldPoint(new Vector3(mousePosScreen.x, mousePosScreen.y, Camera.main.transform.position.z));
        // mousePosWorld.z = 0f;
        // Vector3 direction = mousePosWorld - transform.position;
        // float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        // transform.rotation = Quaternion.Euler(0, 0, angle);
    }
}
/*
    void SummonGrandchild()
    {
        if (canFire)
        {
            var grandChildGo = Instantiate(grandChild, PlayerController.Instance.transform.position, PlayerController.Instance.transform.rotation);
            Destroy(grandChildGo, 3f);

                        timeSinceLastShot = 0;
            canFire = false;
        }
    }

    void AimAtEnemy()
    {
        if (closestEnemy != null)
        {
            Vector3 direction = closestEnemy.position - transform.position;
            direction.Normalize();

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }
*/