using UnityEngine;
using System;

public class PlayerShooting : MonoBehaviour
{
    [SerializeField] private MonoBehaviour inputServiceSource; // drag InputService here
    private IInputService input;

    [SerializeField] private WeaponInventory inventory;

    private bool fireLeftHeld;
    private bool fireRightHeld;

    // cache delegates so we can unsubscribe safely
    private Action onFLStart, onFLCancel, onFRStart, onFRCancel;

    void Awake()
    {
        input = inputServiceSource as IInputService;
        if (input == null) input = FindFirstObjectByType<InputService>(); // Unity 6+
        if (!inventory) inventory = FindFirstObjectByType<WeaponInventory>();
    }

    void OnEnable()
    {
        if (input == null) return;

        onFLStart  = () => fireLeftHeld  = true;
        onFLCancel = () => fireLeftHeld  = false;
        onFRStart  = () => fireRightHeld = true;
        onFRCancel = () => fireRightHeld = false;

        input.FireLeftStarted   += onFLStart;
        input.FireLeftCanceled  += onFLCancel;
        input.FireRightStarted  += onFRStart;
        input.FireRightCanceled += onFRCancel;
    }

    void OnDisable()
    {
        if (input == null) return;
        input.FireLeftStarted   -= onFLStart;
        input.FireLeftCanceled  -= onFLCancel;
        input.FireRightStarted  -= onFRStart;
        input.FireRightCanceled -= onFRCancel;
    }

    void Update()
    {
        if (input == null) return;

        // Compute aim once per frame from mouse or right-stick
        Vector2 aimDir = input.GetAimDirection(transform.position, Camera.main);
        if (aimDir.sqrMagnitude < 0.0001f) aimDir = Vector2.right;

        if (fireLeftHeld)  inventory?.Left?.TryFire(aimDir);
        if (fireRightHeld) inventory?.Right?.TryFire(aimDir);
    }
}