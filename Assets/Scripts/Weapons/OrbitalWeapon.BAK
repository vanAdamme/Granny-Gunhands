// using System.Runtime.CompilerServices;
using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;

public class OrbitalWeapon : SpecialWeaponBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject orbiterPrefab;
    
    [SerializeField] private float damage;
    [SerializeField] private float speed; //degrees per second
    [SerializeField] private float range;
    [SerializeField] private int quantity;
    [SerializeField] private float duration;

    private Transform orbitPoint;
    private List<OrbitalBullet> bulletList = new List<OrbitalBullet>();
    private SpecialWeaponBehaviour bulletScript;
    private bool active = false;
    private bool canActivate;
    public float timer;

    void Start()
    {
        timer = duration;
        canActivate = true;
	}

    void Update()
    {
        if (active)
        {
            timer -= Time.deltaTime;
        }

        if (timer <= 0)
        {
            Deactivate();
            canActivate = false;
        }
	}

	private void Activate()
    {
        if (canActivate)
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

            active = true;
        }
    }

    private void Deactivate()
    {
        foreach (var bullet in bulletList)
        {
            if (bullet != null)
            {
                bullet.die = true;
            }
        }
        bulletList.Clear();
        active = false;
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