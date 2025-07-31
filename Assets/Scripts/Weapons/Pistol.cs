// using System.Collections.Generic;
using UnityEngine;

public class PistolBehaviour : WeaponBehaviour
{
    [Header("Config")]
    // [SerializeField] float fireDistance = 10;
    [SerializeField] private float fireRate = 0.5f;

    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform spawnPoint;


    private float nextFireTime;

    private void Start()
    {
        timeSinceLastShot = fireRate;
    }

    private void Update()
    {
        AimAtMouse();
        CheckTiming();
    }

    void CheckTiming()
    {
        timeSinceLastShot += Time.deltaTime;
        if (timeSinceLastShot >= fireRate)
        {
            canFire = true;
            // timeSinceLastShot = 0;
        }
    }

    void Fire()
    {
        if (canFire)
        {
            var muzzleGo = Instantiate(muzzle, muzzlePosition.position, transform.rotation);
            muzzleGo.transform.SetParent(transform);
            Destroy(muzzleGo, 0.5f);

            var projectileGo = Instantiate(projectile, muzzlePosition.position, transform.rotation);
            Destroy(projectileGo, 3); // make variable

            timeSinceLastShot = 0;
            canFire = false;
        }
    }

    void AimAtMouse()
    {
        // Vector3 mousePosScreen = Input.mousePosition;
        // Vector3 mousePosWorld = Camera.main.ScreenToWorldPoint(new Vector3(mousePosScreen.x, mousePosScreen.y, Camera.main.transform.position.z));
        // mousePosWorld.z = 0f;
        // Vector3 direction = mousePosWorld - transform.position;
        // float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        // transform.rotation = Quaternion.Euler(0, 0, angle);


          Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 direction = (mouseWorldPos - transform.position);
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

}