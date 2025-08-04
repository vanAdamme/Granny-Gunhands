using System.Runtime.CompilerServices;
using UnityEngine;

public class SpinningWeapon : SpecialWeaponBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject spinningBulletPrefab;
    private SpecialWeaponBehaviour bulletScript;

    [SerializeField] private float damage;
    [SerializeField] private float speed;
    [SerializeField] private float range;
    [SerializeField] private int quantity;

    private GameObject[] spinningBullet;
    private bool active = false;

    private void Start()
    {
        // Activate();
    }

    private void Activate()
    {
        // for (int i = 0; i < quantity; i++)
        {
            int i = 0;
            spinningBullet[i] = Instantiate(spinningBulletPrefab, PlayerController.Instance.transform.position * range, transform.rotation);
            SpinningBullet bulletScript = spinningBullet[i].GetComponent<SpinningBullet>();
            bulletScript.Initialise(damage, speed, range);
        }
    }

    public override void ToggleActivate()
    {
        if (active)
        {
            // Deactivate();
            active = false;
        }
        else
        {
            Activate();
            active = true;
        }
    }
}