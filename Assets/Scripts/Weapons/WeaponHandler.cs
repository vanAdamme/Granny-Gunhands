using UnityEngine;

public enum FireButton { LeftClick = 0, RightClick = 1 }

public class WeaponHandler : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private Transform hand;                 // where weapon is parented
    [SerializeField] private GameObject[] weaponPrefabs;     // one handler = one arm’s list

    [Header("Binding")]
    [SerializeField] private FireButton fireButton = FireButton.LeftClick;

    private int currentWeaponIndex = 0;
    private GameObject currentWeaponInstance;
    private Weapon currentWeaponScript;

    void Start()
    {
        if (weaponPrefabs == null || weaponPrefabs.Length == 0)
        {
            Debug.LogWarning($"{name}: No weapon prefabs assigned.");
            return;
        }
        EquipWeapon(0); // first weapon by default
    }

    void Update()
    {
        if (!currentWeaponScript) return;

        AimAtMouse();

        int mouseBtn = (fireButton == FireButton.LeftClick) ? 0 : 1;

        // Per-weapon: FullAuto = hold; SemiAuto = click
        bool wantsFire = (currentWeaponScript.Mode == Weapon.FireMode.FullAuto)
            ? Input.GetMouseButton(mouseBtn)
            : Input.GetMouseButtonDown(mouseBtn);

        if (!wantsFire) return;

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;

        var muzzle = currentWeaponScript.Muzzle ? currentWeaponScript.Muzzle.position : hand.position;
        Vector2 dir = (Vector2)(mouseWorld - muzzle);
        if (dir.sqrMagnitude > 0.0001f)
            currentWeaponScript.TryFire(dir);
    }

    private void EquipWeapon(int index)
    {
        if (index < 0 || index >= weaponPrefabs.Length) return;

        // Destroy old
        if (currentWeaponInstance) Destroy(currentWeaponInstance);

        // Instantiate new
        currentWeaponIndex = index;
        currentWeaponInstance = Instantiate(weaponPrefabs[index], hand);
        currentWeaponScript   = currentWeaponInstance.GetComponent<Weapon>();

        if (!currentWeaponScript)
        {
            Debug.LogError($"{name}: Prefab at index {index} has no Weapon component.");
            return;
        }

        // UI icon per arm
        if (fireButton == FireButton.LeftClick)
            UIController.Instance.UpdateLeftWeaponIcon(currentWeaponScript.icon);
        else
            UIController.Instance.UpdateRightWeaponIcon(currentWeaponScript.icon);
    }

    private void CycleWeapon()
    {
        int nextIndex = (currentWeaponIndex + 1) % weaponPrefabs.Length;
        EquipWeapon(nextIndex);
    }

    private void AimAtMouse()
    {
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0;
        Vector3 dir = mouseWorld - transform.position;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }
}