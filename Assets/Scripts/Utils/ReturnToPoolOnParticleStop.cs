using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class ReturnToPoolOnParticleStop : MonoBehaviour
{
    ParticleSystem ps;
    void Awake() => ps = GetComponent<ParticleSystem>();
    void OnEnable() => ps.Play(true);
    void LateUpdate()
    {
        if (!ps.IsAlive(true))
            GetComponent<PooledObject>()?.Release();
    }
}