using UnityEngine;

public class Bullet : MonoBehaviour
{
	private float damage = 1f;
	private float speed = 12f;
    private float range = 15f;

    private Vector3 startPosition;

    public void Initialise(float damage, float speed, float range)
    {
        this.damage = damage;
        this.speed = speed;
        this.range = range;
        startPosition = transform.position;
    }

	public void SetDamage(float value)
    {
        damage = value;
    }

    private void FixedUpdate()
    {
        transform.Translate(Vector2.right * speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, startPosition) >= range)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.CompareTag("Enemy"))
        {
            Enemy enemy = collider.GetComponent<Enemy>();
            if (enemy != null)
            {
                Destroy(gameObject);
                enemy.TakeDamage(damage);
            }
        }
	}
}