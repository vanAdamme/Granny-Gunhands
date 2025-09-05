using System;
using UnityEngine;
using UnityEngine.InputSystem;

public interface IInputService
{
    // Polled
    Vector2 Move { get; }

    // Edge events
    event Action FireLeftStarted;
    event Action FireLeftCanceled;
    event Action FireRightStarted;
    event Action FireRightCanceled;
    event Action CycleLeft;
    event Action CycleRight;
    event Action Special;
    event Action Interact;
    event Action ToggleInventory;
    event Action Pause;

    // Map switching
    void EnablePlayerMap();
    void EnableUIMap();

    // Pointer + aiming
    Vector2 PointerScreen { get; }
    Vector2 GetAimDirection(Vector3 originWorld, Camera cam = null, float stickDeadzone = 0.2f);
}

[DefaultExecutionOrder(-300)]
public sealed class InputService : MonoBehaviour, IInputService
{
    public static InputService Instance { get; private set; }
    private Controls controls;

    public Vector2 Move { get; private set; }
    public Vector2 PointerScreen { get; private set; }

    public event Action FireLeftStarted;
    public event Action FireLeftCanceled;
    public event Action FireRightStarted;
    public event Action FireRightCanceled;
    public event Action CycleLeft;
    public event Action CycleRight;
    public event Action Special;
    public event Action Interact;
    public event Action ToggleInventory;
    public event Action Pause;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // If on Player prefab, detach so it becomes a root object
        if (transform.parent != null) transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        controls = new Controls();

        // Values
        controls.Player.Move.performed += ctx => Move = ctx.ReadValue<Vector2>();
        controls.Player.Move.canceled  += _   => Move = Vector2.zero;

        // Buttons
        controls.Player.FireLeft.started   += _ => FireLeftStarted?.Invoke();
        controls.Player.FireLeft.canceled  += _ => FireLeftCanceled?.Invoke();
        controls.Player.FireRight.started  += _ => FireRightStarted?.Invoke();
        controls.Player.FireRight.canceled += _ => FireRightCanceled?.Invoke();
        controls.Player.CycleLeft.performed  += _ => CycleLeft?.Invoke();
        controls.Player.CycleRight.performed += _ => CycleRight?.Invoke();
        controls.Player.Special.performed    += _ => Special?.Invoke();
        controls.Player.Interact.performed   += _ => Interact?.Invoke();
        controls.Player.Inventory.performed  += _ => ToggleInventory?.Invoke();
        controls.UI.Inventory.performed     += _ => ToggleInventory?.Invoke();
        controls.Player.Pause.performed      += _ => Pause?.Invoke();
        controls.UI.Pause.performed     += _ => Pause?.Invoke();        
    }

    void OnEnable()
    {
        controls.Enable();
        EnablePlayerMap();
    }

    void OnDisable() => controls.Disable();

    void Update()
    {
        // Track pointer without legacy Input
        if (Mouse.current != null)
            PointerScreen = Mouse.current.position.ReadValue();
    }

    public void EnablePlayerMap()
    {
        controls.UI.Disable();
        controls.Player.Enable();
    }

    public void EnableUIMap()
    {
        controls.Player.Disable();
        controls.UI.Enable();
    }

    public Vector2 GetAimDirection(Vector3 originWorld, Camera cam = null, float stickDeadzone = 0.2f)
    {
        // Prefer right-stick if it’s meaningfully tilted
        if (Gamepad.current != null)
        {
            Vector2 stick = Gamepad.current.rightStick.ReadValue();
            if (stick.sqrMagnitude > stickDeadzone * stickDeadzone)
                return stick.normalized;
        }

        // Fallback to mouse
        cam ??= Camera.main;
        if (!cam) return Vector2.right;

        var sp = PointerScreen;
        var wp = cam.ScreenToWorldPoint(new Vector3(sp.x, sp.y, -cam.transform.position.z));
        Vector2 dir = (Vector2)wp - (Vector2)originWorld;
        return dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.right;
    }
}