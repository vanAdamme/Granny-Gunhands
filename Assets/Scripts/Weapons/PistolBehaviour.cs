// using System.Collections.Generic;
using UnityEngine;

public class PistolBehaviour : WeaponBehaviour
{
    [Header("Config")]
    // [SerializeField] float fireDistance = 10;
    [SerializeField] private float fireRate = 0.5f;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private GameObject muzzleFlashPrefab;
    [SerializeField] private Transform muzzlePoint;

    private float timeSinceLastShot;
    private bool canFire = true;

    private void Start()
    {
        timeSinceLastShot = fireRate;
    }

    private void Update()
    {
        CheckTiming();
    }

    void CheckTiming()
    {
        timeSinceLastShot += Time.deltaTime;
        canFire = timeSinceLastShot >= fireRate;
    }

    public override void Fire()
    {
        if (!canFire) return;

        if (muzzleFlashPrefab)
        {
            var muzzleFlash = Instantiate(muzzleFlashPrefab, muzzlePoint.position, muzzlePoint.rotation);
            // muzzleGo.transform.SetParent(transform);
            Destroy(muzzleFlash, 0.05f);
        }

        var bullet = Instantiate(bulletPrefab, muzzlePoint.position, muzzlePoint.rotation);
        Destroy(bullet, 3); // make variable

        timeSinceLastShot = 0f;
        canFire = false;
    }
}

/*

        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 direction = (mouseWorldPos - transform.position);
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
*/