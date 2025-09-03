using UnityEngine;

[DefaultExecutionOrder(-10000)]
[DisallowMultipleComponent]
public sealed class PersistRootOnAwake : MonoBehaviour
{
    [SerializeField] private bool keepWorldPosition = true;

    void Awake()
    {
        // Must be root for DontDestroyOnLoad to work reliably
        if (transform.parent != null && transform.root != transform)
            transform.SetParent(null, keepWorldPosition);

        DontDestroyOnLoad(gameObject);
    }
}