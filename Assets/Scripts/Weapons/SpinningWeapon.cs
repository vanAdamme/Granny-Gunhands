using System.Runtime.CompilerServices;
using UnityEngine;

public class SpinningWeapon : SpecialWeaponBehaviour
{
    [Header("Prefabs")]
    [SerializeField] Vector3 instantiatePosition;
    [SerializeField] private GameObject bulletPrefab;
    private SpecialWeaponBehaviour bulletScript;

    [SerializeField] private float damage;
    [SerializeField] private float speed;
    [SerializeField] private float range;
    // [SerializeField] private int quantity;

    private void Start()
    {
        Activate();
	}

    public override void Activate()
    {
        var offset = new Vector3(1, 1, 0);
            var bullet = Instantiate(bulletPrefab, PlayerController.Instance.transform.position * range, transform.rotation);
            SpinningBullet bulletScript = bullet.GetComponent<SpinningBullet>();
            bulletScript.Initialise(damage, speed, range);
    }
}