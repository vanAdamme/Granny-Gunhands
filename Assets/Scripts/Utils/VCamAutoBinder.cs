using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CinemachineCamera))]
public class VCamAutoBinder : MonoBehaviour
{
    [SerializeField] Transform overrideTarget;   // optional: drag Player here
    CinemachineCamera vcam;

    void Awake()
    {
        vcam = GetComponent<CinemachineCamera>();

        // Ensure we actually have position control. Add Follow if missing.
        if (GetComponent<CinemachineFollow>() == null)
            gameObject.AddComponent<CinemachineFollow>();
        // (Rotation is up to you: e.g., add CinemachineRotationComposer for aiming)
    }

    void OnEnable()
    {
        TryBind();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    void OnSceneLoaded(Scene s, LoadSceneMode m) => StartCoroutine(BindNextFrame());
    System.Collections.IEnumerator BindNextFrame() { yield return null; TryBind(); }

    void TryBind()
    {
        Transform t = overrideTarget;
        if (!t)
        {
            if (PlayerController.Instance) t = PlayerController.Instance.transform;
            else t = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include)?.transform;
        }

        if (t) vcam.Follow = t;   // CM3: set on the camera, not on the component
    }
}