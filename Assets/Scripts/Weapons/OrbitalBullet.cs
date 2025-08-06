using UnityEngine;

public class OrbitalBullet : MonoBehaviour
{
	private float damage = 1f;
	private float speed = 12f;
    private float range;
    private float angle;
    private Transform center;

    public bool die = false;

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

    private void SetRange(float rangeVal)
    {
        range = rangeVal;
    }

    private void FixedUpdate()
    {
        if (center == null) return;

        if (die)
        {
            range -= 0.2f;
        }

        if (range <= 0)
        {
            Destroy(gameObject);
        }

        // Update angle
            angle += speed * Time.fixedDeltaTime;
        angle %= 360f;

        // Calculate new position
        float rad = angle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * range;
        transform.position = center.position + offset;
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.CompareTag("Enemy"))
        {
            Enemy enemy = collider.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }
        }
    }
}