using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Bullet : MonoBehaviour
{
    private float speed;
    private float range;
    private float damage;

    private GameObject originPrefab;
    private Vector3 startPosition;
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

     private void FixedUpdate()
    {
        transform.Translate(Vector2.right * speed);
        if (Vector3.Distance(transform.position, startPosition) >= range)
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        startPosition = transform.position;
        rb.linearVelocity = transform.right * speed;
    }

    public void SetStats(float damage, float range, GameObject prefabRef)
    {
        this.range = range;
        this.originPrefab = prefabRef;
    }

    public void SetDamage(float value)
    {
        damage = value;
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

            gameObject.SetActive(false); // return to pool
            // AudioController.Instance.PlaySound(AudioController.Instance.directionalWeaponHit);
        }
    }
}