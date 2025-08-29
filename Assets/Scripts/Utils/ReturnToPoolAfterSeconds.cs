using UnityEngine;

public class ReturnToPoolAfterSeconds : MonoBehaviour
{
    [SerializeField] private float lifetime = 1.5f;
    float t;

    void OnEnable() { t = 0f; }
    void Update()
    {
        t += Time.deltaTime;
        if (t >= lifetime)
            GetComponent<PooledObject>()?.Release();
    }
}