using UnityEngine;

public class Egg : MonoBehaviour
{
    private Enemy baby;
    private float hatchTime;
    [SerializeField] private GameObject destroyEffect;

    public void Initialise(Enemy enemy, float duration)
    {
        baby = enemy;
        hatchTime = duration;
    }

    void Update()
    {
        hatchTime -= Time.deltaTime;
        if (hatchTime <= 0)
        {
            Hatch();
            Crack();
        }
    }

    private void Hatch()
    {
        var egg = Instantiate(baby, transform.position, transform.rotation);
    }

    private void Crack()
    {
        Instantiate(destroyEffect, transform.position, transform.rotation);
        Destroy(gameObject);
    }
}
