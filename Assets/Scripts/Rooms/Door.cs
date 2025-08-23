using UnityEngine;

public class Door : MonoBehaviour
{
    [SerializeField] BoxCollider2D blocker;
    [SerializeField] int blockingLayer = -1;   // e.g. LayerMask.NameToLayer("Walls")
    [SerializeField] int nonBlockingLayer = -1; // e.g. LayerMask.NameToLayer("Default")
    [SerializeField] bool startLocked = true;

    int originalLayer;

    void Awake()
    {
        if (!blocker) TryGetComponent(out blocker);
        originalLayer = gameObject.layer;
        if (startLocked) Lock(); else Unlock();
    }

    public void Lock()
    {
        if (blocker) blocker.enabled = true;
        gameObject.layer = blockingLayer >= 0 ? blockingLayer : originalLayer;
    }

    public void Unlock()
    {
        if (blocker) blocker.enabled = false;
        gameObject.layer = nonBlockingLayer >= 0 ? nonBlockingLayer : originalLayer;
    }
}