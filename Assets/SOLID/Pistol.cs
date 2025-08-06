using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Events;

public class Pistol : MonoBehaviour
{
    [Tooltip("Prefab to shoot")]
    [SerializeField] private Projectile projectilePrefab;
    [Tooltip("Damage")]
    [SerializeField] private float damage = 1f;
    [Tooltip("End point of gun where shots appear")]
    [SerializeField] private Transform muzzlePosition;
    [Tooltip("Time between shots / smaller = higher rate of fire")]
    [SerializeField] private float cooldownWindow = 0.1f;
    [SerializeField] private float range = 15;
    [SerializeField] private float speed = 12f;
    [SerializeField] public Sprite icon;
    
    [SerializeField] private UnityEvent m_GunFired;

    // Stack-based ObjectPool available with Unity 2021 and above
    private IObjectPool<Projectile> objectPool;

    // Throw an exception if we try to return an existing item, already in the pool
    [SerializeField] private bool collectionCheck = true;

    // extra options to control the pool capacity and maximum size
    [SerializeField] private int defaultCapacity = 20;
    [SerializeField] private int maxSize = 100;

    private float nextTimeToShoot;

    private void Awake()
    {
        objectPool = new ObjectPool<Projectile>(CreateProjectile,
            OnGetFromPool, OnReleaseToPool, OnDestroyPooledObject,
            collectionCheck, defaultCapacity, maxSize);
    }

    // invoked when creating an item to populate the object pool
    private Projectile CreateProjectile()
    {
        Projectile projectileInstance = Instantiate(projectilePrefab);
        projectileInstance.ObjectPool = objectPool;
        return projectileInstance;
    }

    // Invoked when returning an item to the object pool
    private void OnReleaseToPool(Projectile pooledObject)
    {
        pooledObject.gameObject.SetActive(false);
    }

    // Invoked when retrieving the next item from the object pool
    private void OnGetFromPool(Projectile pooledObject)
    {
        pooledObject.gameObject.SetActive(true);
    }

    // Invoked when we exceed the maximum number of pooled items (i.e. destroy the pooled object)
    private void OnDestroyPooledObject(Projectile pooledObject)
    {
        Destroy(pooledObject.gameObject);
    }

    private void FixedUpdate()
    {
        // Shoot if we have exceeded delay
        if (Input.GetMouseButton(0) && Time.time > nextTimeToShoot && objectPool != null)
        {
            Shoot();
        }
    }

    private void Shoot()
    {
        // Get a pooled object instead of instantiating
        Projectile bulletObject = objectPool.Get();

        if (bulletObject == null)
            return;

        // Align to gun barrel/muzzle position
        bulletObject.transform.SetPositionAndRotation(muzzlePosition.position, muzzlePosition.rotation);
        bulletObject.Initialise(damage, speed, range);
        // Move projectile forward
        // bulletObject.GetComponent<Rigidbody>().AddForce(bulletObject.transform.forward * muzzleVelocity, ForceMode.Acceleration);

        // Turn off after a few seconds
        // bulletObject.Deactivate();

        // Set cooldown delay
        nextTimeToShoot = Time.time + cooldownWindow;
        
        m_GunFired.Invoke();
    }
}