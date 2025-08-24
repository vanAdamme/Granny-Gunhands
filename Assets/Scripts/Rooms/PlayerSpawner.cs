using System.Linq;
using UnityEngine;
using System.Collections;
using System.Reflection;
using UnityEngine.SceneManagement;

[AddComponentMenu("Granny/Spawning/Player Spawner")]
[DefaultExecutionOrder(-100)] // run early
public class PlayerSpawner : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private bool moveExistingPlayer = true;

    [Header("Selection")]
    [SerializeField] private bool requireSafePoint = false;
    [SerializeField] private bool fallbackToUnsafeIfNoneSafe = true;
    [SerializeField] private bool usePriority = true;

    private GameObject cachedPlayer;

    void Awake()
    {
        // If you already have a persistent player in scene
        cachedPlayer = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Exclude)?.gameObject;
    }

    void OnEnable()
    {
        TrySpawnOrMove();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => TrySpawnOrMove();

	public void ForceMoveTo(PlayerSpawnPoint spawn)
	{
		if (spawn == null) return;
		if (cachedPlayer == null) return;

		cachedPlayer.transform.position = spawn.transform.position;
		if (spawn.facing.sqrMagnitude > 0.001f)
		{
			float angle = Mathf.Atan2(spawn.facing.y, spawn.facing.x) * Mathf.Rad2Deg;
			cachedPlayer.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
		}
		AttachCinemachineToPlayer(cachedPlayer.transform);
	}

    private void TrySpawnOrMove()
    {
        var points = FindObjectsByType<PlayerSpawnPoint>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        if (points == null || points.Length == 0)
        {
            Debug.LogWarning("[PlayerSpawner] No PlayerSpawnPoint found in scene.");
            return;
        }

        // Pick best point
        var candidates = usePriority
            ? points.OrderByDescending(p => p.priority).ToArray()
            : points;

        var chosen = requireSafePoint
            ? candidates.FirstOrDefault(p => p.IsSafe()) ?? (fallbackToUnsafeIfNoneSafe ? candidates.FirstOrDefault() : null)
            : candidates.FirstOrDefault(p => p.IsSafe()) ?? candidates.FirstOrDefault();

        if (!chosen) return;

        var pos = chosen.transform.position;
        var facing = chosen.facing;

        if (cachedPlayer != null && moveExistingPlayer)
        {
            cachedPlayer.transform.position = pos;
        }
        else
        {
            if (!playerPrefab)
            {
                Debug.LogError("[PlayerSpawner] No player prefab assigned.");
                return;
            }

            cachedPlayer = Instantiate(playerPrefab, pos, Quaternion.identity);
        }

        // Rotate or orient player if facing given
        if (facing.sqrMagnitude > 0.001f)
        {
            float angle = Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg;
            cachedPlayer.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }

        // Hook up Cinemachine
        AttachCinemachineToPlayer(cachedPlayer.transform);
    }

	private void AttachCinemachineToPlayer(Transform target)
	{
		if (!isActiveAndEnabled || target == null) return;
		// Try immediately, then retry for ~1 second in case vcams/managers enable late
		StartCoroutine(Co_AttachCinemachineWithRetries(target, 30)); // 30 frames ~ 0.5s-1s
	}

	private IEnumerator Co_AttachCinemachineWithRetries(Transform target, int retries)
	{
		while (retries-- > 0 && isActiveAndEnabled)
		{
			if (TryBindToAnyCinemachine(target))
				yield break;
			yield return null; // wait a frame and try again
		}
	}

	// Returns true if something was bound
	private bool TryBindToAnyCinemachine(Transform target)
	{
		bool bound = false;

		var all = Resources.FindObjectsOfTypeAll<MonoBehaviour>();
		for (int i = 0; i < all.Length; i++)
		{
			var mb = all[i];
			if (!mb || !mb.gameObject.scene.IsValid() || mb.hideFlags != HideFlags.None) continue;

			var t = mb.GetType();
			if (t == null) continue;

			string full = t.FullName;

			// --- Case A: Cinemachine 3 camera (Unity.Cinemachine.CinemachineCamera)
			// --- Case B: Cinemachine 2 camera (Cinemachine.CinemachineVirtualCamera)
			if (full == "Unity.Cinemachine.CinemachineCamera" ||
				full == "Cinemachine.CinemachineVirtualCamera")
			{
				// Prefer setting Follow/LookAt props if present
				var followProp = t.GetProperty("Follow", BindingFlags.Instance | BindingFlags.Public);
				var lookAtProp = t.GetProperty("LookAt", BindingFlags.Instance | BindingFlags.Public);

				if (followProp != null && followProp.PropertyType == typeof(Transform))
				{
					followProp.SetValue(mb, target, null);
					bound = true;
				}
				if (lookAtProp != null && lookAtProp.PropertyType == typeof(Transform))
				{
					lookAtProp.SetValue(mb, target, null);
					bound = true;
				}

				if (bound) return true;
			}

			// --- Case C: Cinemachine 3 camera MANAGER (Unity.Cinemachine.CinemachineCameraManagerBase or derived)
			// Needs DefaultTarget.Target.TrackingTarget / LookAtTarget updated
			if (IsSubclassOfFullName(t, "Unity.Cinemachine.CinemachineCameraManagerBase"))
			{
				// DefaultTarget is a public field or property on the type
				var dtField = t.GetField("DefaultTarget", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				var dtProp  = t.GetProperty("DefaultTarget", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

				object dt = dtField != null ? dtField.GetValue(mb) : dtProp?.GetValue(mb);
				if (dt == null) continue;

				var dtType = dt.GetType(); // struct DefaultTargetSettings
				// Enable the default target
				var enabledField = dtType.GetField("Enabled", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				if (enabledField != null) enabledField.SetValue(dt, true);

				// Grab nested "Target" (CameraTarget)
				var targetField = dtType.GetField("Target", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				object camTarget = targetField?.GetValue(dt);
				if (camTarget != null)
				{
					var ctType = camTarget.GetType();
					var trackingField = ctType.GetField("TrackingTarget", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
					var lookAtField   = ctType.GetField("LookAtTarget",   BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
					var customLookAt  = ctType.GetField("CustomLookAtTarget", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

					trackingField?.SetValue(camTarget, target);
					if (lookAtField != null)  lookAtField.SetValue(camTarget, target);
					if (customLookAt != null) customLookAt.SetValue(camTarget, true);

					// write back the nested struct, then the outer struct (because structs are value types)
					targetField?.SetValue(dt, camTarget);
				}

				// write back DefaultTarget to the component instance
				if (dtField != null) dtField.SetValue(mb, dt);
				else if (dtProp != null && dtProp.CanWrite) dtProp.SetValue(mb, dt);

				bound = true;
				// don't break; continue to set multiple managers/cameras if present
			}
		}

		return bound;
	}

	private static bool IsSubclassOfFullName(System.Type t, string fullName)
	{
		for (var cur = t; cur != null; cur = cur.BaseType)
			if (cur.FullName == fullName) return true;
		return false;
	}
}