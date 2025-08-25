using UnityEngine;

/// <summary>
/// Fades out a SpriteRenderer over a given duration, then destroys the GameObject.
/// Great for debris, splinters, or temporary props.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class FadeAndDestroy : MonoBehaviour
{
    [SerializeField] private float delayBeforeFade = 1.5f;
    [SerializeField] private float fadeDuration = 0.75f;
    [SerializeField] private bool destroyOnDisable = true;

    private SpriteRenderer sr;
    private float timer;
    private bool fading;
    private Color startColor;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        startColor = sr.color;
    }

    void OnEnable()
    {
        // Reset when reused from pool
        timer = 0f;
        fading = false;
        if (sr) sr.color = startColor;
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (!fading && timer >= delayBeforeFade)
        {
            fading = true;
            timer = 0f; // restart timer for fade phase
        }

        if (fading)
        {
            float t = timer / Mathf.Max(0.01f, fadeDuration);
            Color c = sr.color;
            c.a = Mathf.Lerp(startColor.a, 0f, t);
            sr.color = c;

            if (t >= 1f)
            {
                Destroy(gameObject);
            }
        }
    }

    void OnDisable()
    {
        if (destroyOnDisable && fading == false && Application.isPlaying)
        {
            // Ensure we don't leave faded shards hanging around when pooling
            Destroy(gameObject);
        }
    }
}