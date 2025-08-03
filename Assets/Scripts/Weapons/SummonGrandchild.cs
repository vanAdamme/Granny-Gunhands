using UnityEngine;

public class SummonGrandchild : WeaponBehaviour
{
    [Header("Config")]
    [SerializeField] private float damage;
    [SerializeField] private float speed;
    [SerializeField] private float range;

    // [SerializeField] float range;
    [SerializeField] private float fireRate;
    [SerializeField] private GameObject grandchildPrefab;

    private float timeSinceLastShot;
    private bool canFire = true;

    private void Start()
    {
        timeSinceLastShot = fireRate;
    }

    protected override void Update()
    {
        base.Update();
        CheckTiming();
    }

    void CheckTiming()
    {
        timeSinceLastShot += Time.deltaTime;
        canFire = timeSinceLastShot >= fireRate;
    }

    public override void Fire()
    {
        if (!canFire) return;
        var grandchild = Instantiate(grandchildPrefab, transform.position, transform.rotation);
        Grandchild grandchildScript = grandchild.GetComponent<Grandchild>();
        grandchildScript.Initialise(damage, speed, range);
 
        timeSinceLastShot = 0f;
        canFire = false;
    }
}
