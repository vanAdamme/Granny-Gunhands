using UnityEngine;


public class objBounce : MonoBehaviour
{
    public float bounceHeight = 0.25f;
    public float bounceSpeed = 3f;

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        float newY = Mathf.Sin(Time.time * bounceSpeed) * bounceHeight;
        transform.position = startPos + new Vector3(0f, newY, 0f);
    }
}