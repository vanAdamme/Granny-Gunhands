using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : Target, IPlayerContext
{
    public static PlayerController Instance;

    [Header("Refs")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator animator;
    public Collider2D col;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    public Vector2 playerMoveDirection;

    [Header("Progression")]
    public int experience;
    public int currentLevel;
    public int maxLevel;
    public List<int> playerLevels;

    [Header("Inventories")]
    [Tooltip("This must be the SAME ItemInventory instance your Inventory UI uses.")]
    [SerializeField] private ItemInventory itemInventory;
    [SerializeField] private WeaponInventory weaponInventory;

    [Header("Damage / Invulnerability")]
    [Tooltip("0 = permanent")]
    [SerializeField] private float invulnerabilityDuration = 0.5f;
    private float invulnerabilityTimer;

    private IInputService input; // resolved in Awake

    // ===== IPlayerContext implementation =====
    public float MoveSpeed
    {
        get => moveSpeed;
        set => moveSpeed = value;
    }

    public new float MaxHealth
    {
        get => base.MaxHealth;
        set => base.MaxHealth = value;
    }

    public new bool IsInvulnerable
    {
        get => base.IsInvulnerable;
        set => base.IsInvulnerable = value;
    }

    public bool TryGetActiveWeapon(Hand hand, out Weapon w)
    {
        w = weaponInventory ? weaponInventory.GetWeapon(hand) : null;
        return w != null;
    }

    public bool TryGetActiveWeapon<T>(Hand hand, out T w) where T : Weapon
    {
        w = weaponInventory ? weaponInventory.GetWeapon(hand) as T : null;
        return w != null;
    }

    public Transform Transform => transform;

    // NEW: expose the inventory to everything (pickups, UI, item usage)
    public ItemInventory ItemInventory => itemInventory;
    // =========================================

    protected override void Awake()
    {
        base.Awake();
        col = GetComponent<Collider2D>();

        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;

        // Resolve input
        input = InputService.Instance as IInputService;
        if (input == null)
        {
            input = FindFirstObjectByType<InputService>(); // Unity 6+ safe API
            if (input == null) Debug.LogError("InputService not found in scene.");
        }

        // Sanity: warn if inventory isn't wired
        if (!itemInventory)
        {
            itemInventory = GetComponentInChildren<ItemInventory>();
            if (!itemInventory)
                Debug.LogError("[PlayerController] ItemInventory is not assigned. Drag the SAME instance the UI uses.");
        }
    }

    private void Start()
    {
        // Fill levels list up to maxLevel if needed
        for (int i = playerLevels.Count; i < maxLevel; i++)
            playerLevels.Add(Mathf.CeilToInt(playerLevels[playerLevels.Count - 1] * 1.1f + 15));

        UIController.Instance.UpdateHealthSlider();
        UIController.Instance.UpdateExperienceSlider();

        if (weaponInventory != null)
        {
            weaponInventory.OnEquippedChanged += (hand, weapon) =>
            {
                if (!UIController.Instance) return;
                var icon = weapon ? weapon.icon : null;

                if (hand == Hand.Left)  UIController.Instance.UpdateLeftWeaponIcon(icon);
                else                    UIController.Instance.UpdateRightWeaponIcon(icon);
            };

            // Push initial icons
            if (weaponInventory.Left)   UIController.Instance.UpdateLeftWeaponIcon(weaponInventory.Left.icon);
            if (weaponInventory.Right)  UIController.Instance.UpdateRightWeaponIcon(weaponInventory.Right.icon);
        }
    }

    private void Update()
    {
        // Anim from input.Move
        playerMoveDirection = (input != null) ? input.Move : Vector2.zero;

        if (playerMoveDirection == Vector2.zero)
        {
            animator.SetBool("moving", false);
        }
        else if (Time.timeScale != 0f)
        {
            animator.SetBool("moving", true);
            animator.SetFloat("moveX", playerMoveDirection.x);
            animator.SetFloat("moveY", playerMoveDirection.y);
        }

        // Invulnerability countdown
        if (invulnerabilityTimer > 0f)
        {
            invulnerabilityTimer -= Time.deltaTime;
            if (invulnerabilityTimer <= 0f) SetInvulnerable(false);
        }
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = playerMoveDirection * moveSpeed;
    }

    public override void TakeDamage(float amount)
    {
        if (IsInvulnerable) return;

        base.TakeDamage(amount);
        UIController.Instance.UpdateHealthSlider();
        SetInvulnerable(true);
    }

    protected override void Die()
    {
        base.Die();
        GameManager.Instance.GameOver();
    }

    private void SetInvulnerable(bool state)
    {
        IsInvulnerable = state;
        var sr = GetComponent<SpriteRenderer>();
        if (state)
        {
            invulnerabilityTimer = invulnerabilityDuration;
            if (sr)
            {
                var c = sr.color; c.a = 0.8f; sr.color = c;
            }
        }
        else
        {
            invulnerabilityTimer = 0f;
            if (sr)
            {
                var c = sr.color; c.a = 1f; sr.color = c;
            }
        }
    }

    // ---------- Input wiring ----------
    void OnEnable()
    {
        if (input == null) return;
        input.CycleLeft  += OnCycleLeft;
        input.CycleRight += OnCycleRight;
    }

    void OnDisable()
    {
        if (input == null) return;
        input.CycleLeft  -= OnCycleLeft;
        input.CycleRight -= OnCycleRight;
    }

    private void OnCycleLeft()  => weaponInventory?.Cycle(Hand.Left,  +1);
    private void OnCycleRight() => weaponInventory?.Cycle(Hand.Right, +1);
    private void OnSpecial()    { /* trigger your special when wired */ }

    // NOTE: removed the InputAction.CallbackContext overloads to avoid delegate mismatches
    // ---------- end input wiring ----------

    public override void Heal(float amount)
    {
        Debug.Log($"[PlayerController] Heal({amount}) before={CurrentHealth}");
        base.Heal(amount);
        UIController.Instance.UpdateHealthSlider();
    }

    public void AddExperience(int amount)
    {
        experience += amount;
        UIController.Instance.UpdateExperienceSlider();
        // if (experience >= playerLevels[currentLevel - 1]) { ... level-up flow ... }
    }

    public void IncreaseMaxHealth(int value)
    {
        MaxHealth += value;
        base.Heal(value); // top-up by the same amount
        UIController.Instance.UpdateHealthSlider();

        UIController.Instance.LevelUpPanelClose();
    }

    public void IncreaseMovementSpeed(float multiplier)
    {
        moveSpeed *= multiplier;
        UIController.Instance.LevelUpPanelClose();
    }
}