// using UnityEditor.XR;
using UnityEngine;

public enum FireButton
{
    LeftClick = 0,
    RightClick = 1
}

public class ArmController : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] GameObject muzzle;
    [SerializeField] Transform muzzlePosition;
    [SerializeField] GameObject projectile;

    [Header("Config")]
    // [SerializeField] float fireDistance = 10;
    [SerializeField] float fireRate = 0.5f;

    [SerializeField] private FireButton fireButton;

    private float timeSinceLastShot = 0f;
    private bool canShoot = false;
    Transform closestEnemy;
        [SerializeField] GameObject grandChild;
    Animator anim;

    void Start()
    {
        anim = GetComponent<Animator>();
        timeSinceLastShot = fireRate;
    }

    void Update()
    {
        AimAtMouse();
        CheckTiming();

        if (Input.GetMouseButton((int)fireButton))
        {
            // Shoot();
            SummonGrandchild();
        }


    }

    void AimAtMouse()
    {
        Vector3 mousePosScreen = Input.mousePosition;
        Vector3 mousePosWorld = Camera.main.ScreenToWorldPoint(new Vector3(mousePosScreen.x, mousePosScreen.y, Camera.main.transform.position.z));
        mousePosWorld.z = 0f;
        Vector3 direction = mousePosWorld - transform.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    void CheckTiming()
    {
        timeSinceLastShot += Time.deltaTime;
        if (timeSinceLastShot >= fireRate)
        {
            canShoot = true;
            // timeSinceLastShot = 0;
        }
    }

    void Shoot()
    {
        if (canShoot)
        {
            var muzzleGo = Instantiate(muzzle, muzzlePosition.position, transform.rotation);
            muzzleGo.transform.SetParent(transform);
            Destroy(muzzleGo, 0.5f);

            var projectileGo = Instantiate(projectile, muzzlePosition.position, transform.rotation);
            Destroy(projectileGo, 3); // make variable

            timeSinceLastShot = 0;
            canShoot = false;
        }
    }

    void SummonGrandchild()
    {
        if (canShoot)
        {
            var grandChildGo = Instantiate(grandChild, PlayerController.Instance.transform.position, PlayerController.Instance.transform.rotation);
            Destroy(grandChildGo, 3f);

                        timeSinceLastShot = 0;
            canShoot = false;
        }
    }

    
/*
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
}