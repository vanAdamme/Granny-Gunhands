using UnityEngine;

public class Chicken : MonoBehaviour
{
    [SerializeField] private GameObject eggPrefab;
    [SerializeField] private Enemy baby;
    [SerializeField] private float hatchTime;

    private void LayEgg()
    {
        var egg = Instantiate(eggPrefab, transform.position, transform.rotation);
        Egg eggScript = egg.GetComponent<Egg>();
        eggScript.Initialise(baby, hatchTime);
    }
}