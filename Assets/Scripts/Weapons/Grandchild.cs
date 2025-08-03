using UnityEngine;

public class Grandchild : MonoBehaviour
{
	private float damage = 1f;
	private float speed = 12f;
    private float range = 15f;

    private Vector3 startPosition;
    Transform closestEnemy;

    public void Initialise(float damage, float speed, float range)
    {
        this.damage = damage;
        this.speed = speed;
        this.range = range;
        startPosition = transform.position;
    }

    private void Start()
    {
        FindClosestEnemy();
    }

    private void FixedUpdate()
    {
        transform.position = Vector3.MoveTowards(transform.position, closestEnemy.transform.position, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, startPosition) >= range)
        {
            Destroy(gameObject);
        }
    }

    void FindClosestEnemy()
    {
        closestEnemy = null;
        float closestEnemyDist;

        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);

        closestEnemyDist = Vector2.Distance(PlayerController.Instance.transform.position, enemies[0].transform.position);

        foreach (Enemy enemy in enemies)
        {
            float distance = Vector2.Distance(PlayerController.Instance.transform.position, enemy.transform.position);
            if (distance <= closestEnemyDist)
            {
                closestEnemy = enemy.transform;
                closestEnemyDist = distance;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.gameObject.CompareTag("Enemy"))
        {
            Enemy enemy = collider.gameObject.GetComponent<Enemy>();
            Destroy(gameObject);
            enemy.TakeDamage(damage); //make variable
            // AudioController.Instance.PlaySound(AudioController.Instance.directionalWeaponHit);
        }
    }
}