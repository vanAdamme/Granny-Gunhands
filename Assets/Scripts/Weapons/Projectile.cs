using UnityEngine;

public class Projectile : MonoBehaviour
{
    float speed = 12f; // make variable

    private void FixedUpdate()
    {
        transform.Translate(Vector2.right * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.gameObject.CompareTag("Enemy"))
        {
            Enemy enemy = collider.gameObject.GetComponent<Enemy>();
            Destroy(gameObject);
            enemy.TakeDamage(1); //make variable
            // enemy.TakeDamage(weapon.stats[weapon.weaponLevel].damage);
            // AudioController.Instance.PlaySound(AudioController.Instance.directionalWeaponHit);
        }
	}
}