using UnityEngine;
using UnityEngine.Pool;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class BulletKinetic : MonoBehaviour, IPooledRelease
{
    Rigidbody2D rb;
    Vector2 dir;
    Vector2 startPos;
    float speed;
    float maxDistance;
    float maxDistanceSqr;
    LayerMask wallLayers;
    LayerMask targetLayers;

    IObjectPool<BulletKinetic> pool;
    bool initialised;
    bool released;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        // Prefab hints:
        // BodyType=Dynamic, GravityScale=0, CollisionDetection=Continuous, Interpolate=Interpolate
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    void OnEnable()
    {
        startPos = rb ? rb.position : (Vector2)transform.position;
        initialised = false; // wait for Init to set fields each reuse
        released = false;
    }

    public void Init(Vector2 direction, float speed, float range, LayerMask wallLayers, LayerMask targetLayers, IObjectPool<BulletKinetic> pool)
    {
        this.dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
        this.speed = speed;
        this.maxDistance = Mathf.Max(0f, range);
        this.maxDistanceSqr = this.maxDistance * this.maxDistance;
        this.wallLayers = wallLayers;
        this.targetLayers = targetLayers;
        this.pool = pool;

        if (rb) rb.linearVelocity = this.dir * this.speed;
        initialised = true;
    }

    void FixedUpdate()
    {
        if (released || !initialised || !rb) return;

        // keep stable velocity (in case physics touches it)
        rb.linearVelocity = dir * speed;

        // range cutoff
        if (((Vector2)rb.position - startPos).sqrMagnitude >= maxDistanceSqr)
            Release();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (released || !initialised) return;

        int layer = other.gameObject.layer;

        // Stop on walls
        if ((wallLayers.value & (1 << layer)) != 0)
        {
            Release();
            return;
        }

        // Despawn on damaging a valid target — Hitbox handles the actual damage
        if ((targetLayers.value & (1 << layer)) != 0)
        {
            Release();
        }
    }

    public void Release()
    {
        if (released) return;

        released = true;

        if (rb)
        {
            rb.linearVelocity = Vector2.zero;
        }

        if (pool != null)
        {
            pool.Release(this);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}