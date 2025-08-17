using UnityEngine;

public class MountAimer : MonoBehaviour
{
    Camera cam;
    void Awake() => cam = Camera.main;

    void LateUpdate()
    {
        if (!cam) return;
        Vector3 m = cam.ScreenToWorldPoint(Input.mousePosition); m.z = 0;
        Vector2 dir = (m - transform.position);
        if (dir.sqrMagnitude < 0.0001f) return;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }
}