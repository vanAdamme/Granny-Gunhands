using UnityEngine;

public class PistolBehaviour : WeaponBehaviour
{
    [Header("Config")]
    [SerializeField] private float damage;
    [SerializeField] private float speed;
    [SerializeField] private float range;

    [SerializeField] private float fireRate;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private GameObject muzzleFlashPrefab;
    [SerializeField] private Transform muzzlePoint;

    private float timeSinceLastShot;
    private bool canFire = true;

    private void Start()
    {
        timeSinceLastShot = fireRate;
    }

    protected override void Update()
    {
        base.Update();
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
            var muzzleFlash = Instantiate(muzzleFlashPrefab, muzzlePoint.position, transform.rotation);
            Destroy(muzzleFlash, 0.05f);
        }

        var bullet = Instantiate(bulletPrefab, muzzlePoint.position, transform.rotation);
        Bullet bulletScript = bullet.GetComponent<Bullet>();
        bulletScript.Initialise(damage, speed, range);

        timeSinceLastShot = 0f;
        canFire = false;
    }
}