using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class Projectile : MonoBehaviour
{
    private float damage = 1f;
	private float speed = 12f;
    private float range = 15f;
    private Vector2 startPosition;

    private IObjectPool<Projectile> objectPool;

    // Public property to give the projectile a reference to its ObjectPool
    public IObjectPool<Projectile> ObjectPool { set => objectPool = value; }

    public void Initialise(float damage, float speed, float range)
    {
        this.damage = damage;
        this.speed = speed;
        this.range = range;
        startPosition = transform.position;
    }

    private void FixedUpdate()
	{
		transform.Translate(Vector2.right * speed * Time.deltaTime);

		if (Vector2.Distance(transform.position, startPosition) > range)
		{
			Deactivate();
		}
	}

    private void OnTriggerEnter2D(Collider2D collider)
    {
        IDamageable damageable = collider.GetComponent<IDamageable>();
        if (damageable != null)
        {
            Debug.Log("target hit");
            damageable.TakeDamage(damage);
            Deactivate();
        }
	}

    public void Deactivate()
    {
        // Release the projectile back to the pool
        objectPool.Release(this);
    }
}