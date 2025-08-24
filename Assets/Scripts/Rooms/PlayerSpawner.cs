using System.Linq;
using UnityEngine;
using System.Collections;
using System.Reflection;
using UnityEngine.SceneManagement;

[AddComponentMenu("Granny/Spawning/Player Spawner")]
[DefaultExecutionOrder(-100)]
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
        cachedPlayer = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Exclude)?.gameObject;
    }

    void OnEnable()
    {
        TrySpawnOrMove();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;
    private void OnSceneLoaded(Scene s, LoadSceneMode m) => TrySpawnOrMove();

    public void ForceMoveTo(PlayerSpawnPoint spawn)
    {
        if (!spawn || cachedPlayer == null) return;

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
            Debug.LogWarning("[PlayerSpawner] No PlayerSpawnPoint found.");
            return;
        }

        var candidates = usePriority ? points.OrderByDescending(p => p.priority).ToArray() : points;
        var chosen = requireSafePoint
            ? candidates.FirstOrDefault(p => p.IsSafe()) ?? (fallbackToUnsafeIfNoneSafe ? candidates.FirstOrDefault() : null)
            : candidates.FirstOrDefault(p => p.IsSafe()) ?? candidates.FirstOrDefault();

        if (!chosen) return;

        if (cachedPlayer != null && moveExistingPlayer)
            cachedPlayer.transform.position = chosen.transform.position;
        else
        {
            if (!playerPrefab) { Debug.LogError("[PlayerSpawner] No player prefab."); return; }
            cachedPlayer = Instantiate(playerPrefab, chosen.transform.position, Quaternion.identity);
        }

        if (chosen.facing.sqrMagnitude > 0.001f)
        {
            float angle = Mathf.Atan2(chosen.facing.y, chosen.facing.x) * Mathf.Rad2Deg;
            cachedPlayer.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }

        AttachCinemachineToPlayer(cachedPlayer.transform);
    }

    private void AttachCinemachineToPlayer(Transform target)
    {
        if (!isActiveAndEnabled || target == null) return;
        StartCoroutine(Co_AttachCinemachineWithRetries(target, 30));
    }

    private IEnumerator Co_AttachCinemachineWithRetries(Transform target, int retries)
    {
        while (retries-- > 0 && isActiveAndEnabled)
        {
            if (TryBindToAnyCinemachine(target)) yield break;
            yield return null;
        }
    }

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

            if (full == "Unity.Cinemachine.CinemachineCamera" || full == "Cinemachine.CinemachineVirtualCamera")
            {
                var followProp = t.GetProperty("Follow", BindingFlags.Instance | BindingFlags.Public);
                var lookAtProp = t.GetProperty("LookAt", BindingFlags.Instance | BindingFlags.Public);

                if (followProp?.PropertyType == typeof(Transform)) { followProp.SetValue(mb, target, null); bound = true; }
                if (lookAtProp?.PropertyType == typeof(Transform)) { lookAtProp.SetValue(mb, target, null); bound = true; }
                if (bound) return true;
            }

            if (IsSubclassOfFullName(t, "Unity.Cinemachine.CinemachineCameraManagerBase"))
            {
                var dtField = t.GetField("DefaultTarget", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var dtProp  = t.GetProperty("DefaultTarget", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                object dt   = dtField != null ? dtField.GetValue(mb) : dtProp?.GetValue(mb);
                if (dt == null) continue;

                var dtType = dt.GetType();
                dtType.GetField("Enabled", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                      ?.SetValue(dt, true);

                var targetField = dtType.GetField("Target", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                object camTarget = targetField?.GetValue(dt);
                if (camTarget != null)
                {
                    var ctType = camTarget.GetType();
                    ctType.GetField("TrackingTarget", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                          ?.SetValue(camTarget, target);
                    ctType.GetField("LookAtTarget",   BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                          ?.SetValue(camTarget, target);
                    ctType.GetField("CustomLookAtTarget", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                          ?.SetValue(camTarget, true);
                    targetField?.SetValue(dt, camTarget);
                }

                if (dtField != null) dtField.SetValue(mb, dt);
                else if (dtProp != null && dtProp.CanWrite) dtProp.SetValue(mb, dt);

                bound = true;
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