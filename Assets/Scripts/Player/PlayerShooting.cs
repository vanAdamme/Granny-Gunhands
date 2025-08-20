using UnityEngine;
using System;

public class PlayerShooting : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private MonoBehaviour inputServiceSource;   // drag InputService here (implements IInputService)
    [SerializeField] private WeaponInventory inventory;          // prefer wiring in Inspector
    [SerializeField] private Camera aimCamera;                   // cache, don’t use Camera.main every frame

    private IInputService input;

    // Fire state (held)
    private bool fireLeftHeld;
    private bool fireRightHeld;

    // Keep last non-zero aim to avoid snapping to +X when input is momentarily zero
    private Vector2 lastAimDir = Vector2.right;

    // Cached delegates so we can unsubscribe safely
    private Action onFLStart, onFLCancel, onFRStart, onFRCancel;

    void Awake()
    {
        // Input: prefer serialized reference; fallback to singleton or scene search
        input = inputServiceSource as IInputService
             ?? InputService.Instance as IInputService
             ?? FindFirstObjectByType<InputService>(); // Unity 6+

        // Inventory: prefer local; fallback up the hierarchy; final fallback global
        if (!inventory) inventory = GetComponentInParent<WeaponInventory>();
        if (!inventory) inventory = FindFirstObjectByType<WeaponInventory>();

        // Cache a camera (don’t tag-scan every Update)
        if (!aimCamera) aimCamera = Camera.main;

        // Prepare delegates once
        onFLStart  = () => fireLeftHeld  = true;
        onFLCancel = () => fireLeftHeld  = false;
        onFRStart  = () => fireRightHeld = true;
        onFRCancel = () => fireRightHeld = false;
    }

    void OnEnable()
    {
        if (input == null) return;
        input.FireLeftStarted   += onFLStart;
        input.FireLeftCanceled  += onFLCancel;
        input.FireRightStarted  += onFRStart;
        input.FireRightCanceled += onFRCancel;
    }

    void OnDisable()
    {
        if (input == null) return;
        if (onFLStart  != null) input.FireLeftStarted   -= onFLStart;
        if (onFLCancel != null) input.FireLeftCanceled  -= onFLCancel;
        if (onFRStart  != null) input.FireRightStarted  -= onFRStart;
        if (onFRCancel != null) input.FireRightCanceled -= onFRCancel;
    }

    void Update()
    {
        if (input == null) return;

        // Compute aim once per frame from stick or mouse via the service.
        // Use the player pivot as origin; you could also pass a muzzle if you want exact rays from gun barrels.
        Vector2 dir = input.GetAimDirection(transform.position, aimCamera);
        if (dir.sqrMagnitude > 0.0001f)
            lastAimDir = dir; // persist last meaningful aim

        // Fire while held; weapon cooldowns will gate actual shots.
        if (fireLeftHeld)  inventory?.Left?.TryFire(lastAimDir);
        if (fireRightHeld) inventory?.Right?.TryFire(lastAimDir);
    }
}