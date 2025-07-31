using UnityEngine;

public class Grandchild : MonoBehaviour
{
    Transform closestEnemy;
    [SerializeField] GameObject grandChild;
    public float duration = 3f;

    float speed = 12f; // make variable

    private void Start()
    {
        FindClosestEnemy();
        // Destroy(gameObject, duration);
	}

    private void FixedUpdate()
    {
         transform.position = Vector3.MoveTowards(transform.position, closestEnemy.transform.position, speed * Time.deltaTime);
        // transform.Translate(Vector2.right * speed * Time.deltaTime);
    }

    void FindClosestEnemy()
    {
        closestEnemy = null;
        float closestEnemyDist;

        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);

        closestEnemyDist = Vector2.Distance(PlayerController.Instance.transform.position, enemies[0].transform.position);

        foreach (Enemy enemy in enemies)
        {
            float _distance = Vector2.Distance(PlayerController.Instance.transform.position, enemy.transform.position);
            if (_distance <= closestEnemyDist)
            {
                closestEnemy = enemy.transform;
                closestEnemyDist = _distance;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.gameObject.CompareTag("Enemy"))
        {
            Enemy enemy = collider.gameObject.GetComponent<Enemy>();
            Destroy(gameObject);
            enemy.TakeDamage(5); //make variable
            // enemy.TakeDamage(weapon.stats[weapon.weaponLevel].damage);
            // AudioController.Instance.PlaySound(AudioController.Instance.directionalWeaponHit);
        }
    }
}