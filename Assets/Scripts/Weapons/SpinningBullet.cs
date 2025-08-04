using UnityEngine;

public class SpinningBullet : MonoBehaviour
{
	private float damage = 1f;
	private float speed = 12f;
    private float range = 15f;
    private Transform rotationCentre;

    public void Initialise(float damage, float speed, float range)
    {
        this.damage = damage;
        this.speed = speed;
        this.range = range;
    }

	public void SetDamage(float value)
	{
		damage = value;
	}

    private void FixedUpdate()
    {
        Debug.Log(range);
        rotationCentre = PlayerController.Instance.transform.Find("Bun rotation point");
        this.transform.RotateAround(rotationCentre.position * range, Vector3.forward, speed * Time.deltaTime);

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
            AudioController.Instance.PlaySound(AudioController.Instance.directionalWeaponHit);
        }
	}
}