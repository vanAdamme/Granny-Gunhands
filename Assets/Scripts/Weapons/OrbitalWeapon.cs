// using System.Runtime.CompilerServices;
using UnityEngine;
using System.Collections.Generic;

public class OrbitalWeapon : SpecialWeaponBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject orbiterPrefab;
    
    [SerializeField] private float damage;
    [SerializeField] private float speed; //degrees per second
    [SerializeField] private float range;
    [SerializeField] private int quantity;

    private Transform orbitPoint;
    private List<OrbitalBullet> bulletList = new List<OrbitalBullet>();
    private SpecialWeaponBehaviour bulletScript;
    private bool active = false;

    private void Activate()
    {
        if (orbitPoint == null)
        {
            orbitPoint = PlayerController.Instance.transform.Find("Bun");

            if (orbitPoint == null)
            {
                Debug.LogError("OrbitPoint not found on player.");
                return;
            }
        }

        for (int i = 0; i < quantity; i++)
        {
            float angle = (360f / quantity) * i;

            GameObject bulletGO = Instantiate(orbiterPrefab);
            OrbitalBullet bullet = bulletGO.GetComponent<OrbitalBullet>();
            bullet.Initialise(orbitPoint, damage, speed, range, angle);
            bulletList.Add(bullet);
        }
    }

    private void Deactivate()
    {
        foreach (var bullet in bulletList)
        {
            if (bullet != null)
                Destroy(bullet.gameObject);
        }
        bulletList.Clear();
    }

    public override void ToggleActivate()
    {
        if (active)
        {
            Deactivate();
            active = false;
        }
        else
        {
            Activate();
            active = true;
        }
    }
}