using UnityEngine;

public abstract class WeaponBehaviour : MonoBehaviour
{
    [SerializeField] protected SpriteRenderer spriteRenderer;
    [SerializeField] public Sprite icon;
    [SerializeField] protected AudioClip fireClip;

    public abstract void Fire();

    protected virtual void Awake()
    {
        spriteRenderer ??= GetComponentInChildren<SpriteRenderer>();
    }

    protected virtual void Update()
    {
        FlipSprite();
    }

    protected void FlipSprite()
    {
        if (PlayerController.Instance == null) return;

        Vector3 scale = transform.localScale;

        if (transform.position.x > PlayerController.Instance.transform.position.x)
        {
            scale.y = Mathf.Abs(scale.y);
        }
        else
        {
            scale.y = -Mathf.Abs(scale.y);
        }

        transform.localScale = scale;
    }

    protected void Bang()
    {
        // Create a temporary object just for the death sound
        GameObject soundObj = new GameObject("Bang");
        AudioSource tempSource = soundObj.AddComponent<AudioSource>();
        tempSource.clip = fireClip;
        tempSource.Play();

        // Destroy the sound object after the clip ends
        Destroy(soundObj, fireClip.length);
    }
}