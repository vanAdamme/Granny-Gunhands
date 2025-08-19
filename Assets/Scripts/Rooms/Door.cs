using UnityEngine;

public class Door : MonoBehaviour, ILockable
{
    [SerializeField] private Collider2D blocker; // e.g., BoxCollider2D acting as a wall
    [SerializeField] private Animator animator;

    public bool IsLocked { get; private set; }

    public void Lock()
    {
        IsLocked = true;
        if (blocker) blocker.enabled = true;
        if (animator) animator.SetBool("open", false);
    }

    public void Unlock()
    {
        IsLocked = false;
        if (blocker) blocker.enabled = false;
        if (animator) animator.SetBool("open", true);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!blocker) TryGetComponent(out blocker);
    }
#endif
}