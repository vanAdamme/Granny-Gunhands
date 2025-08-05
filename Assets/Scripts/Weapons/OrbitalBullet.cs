using UnityEngine;

public class OrbitalBullet : MonoBehaviour
{
	private float damage = 1f;
	private float speed = 12f;
    private float range = 15f;
    private float angle;
    private Transform center;

    public void Initialise(Transform centerPos, float damageVal, float speedVal, float rangeVal, float angleVal)
    {
        damage = damageVal;
        speed = speedVal;
        range = rangeVal;
        center = centerPos;
        angle = angleVal;

        // Set initial position
        float rad = angle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * range;
        transform.position = center.position + offset;
    }

    private void FixedUpdate()
    {
        if (center == null) return;

        // Update angle
        angle += speed * Time.fixedDeltaTime;
        angle %= 360f;

        // Calculate new position
        float rad = angle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * range;
        transform.position = center.position + offset;

        //if (time > duration)
        // {
        //     Destroy(gameObject);
        // }
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