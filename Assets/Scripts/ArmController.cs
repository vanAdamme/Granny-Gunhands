using UnityEngine;

public class ArmController : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] GameObject muzzle;
    [SerializeField] Transform muzzlePosition;
    [SerializeField] GameObject projectile;

    [Header("Config")]
    [SerializeField] float fireDistance = 10;
    [SerializeField] float fireRate = 0.5f;

    private float timeSinceLastShot = 0f;
    Transform closestEnemy;
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

        if (Input.GetMouseButtonDown(0))
        {
            Shoot();
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
            timeSinceLastShot = 0;
        }
    }

    void Shoot()
    {
        var muzzleGo = Instantiate(muzzle, muzzlePosition.position, transform.rotation);
        muzzleGo.transform.SetParent(transform);
        Destroy(muzzleGo, 0.5f);

        var projectileGo = Instantiate(projectile, muzzlePosition.position, transform.rotation);
        Destroy(projectileGo, 3); // make variable
    }

    void FindClosestEnemy()
    {
        closestEnemy = null;

        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);

        foreach (Enemy enemy in enemies)
        {
            float distance = Vector2.Distance(transform.position, enemy.transform.position);
            if (distance <= fireDistance)
            {
                closestEnemy = enemy.transform;
            }
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
}
