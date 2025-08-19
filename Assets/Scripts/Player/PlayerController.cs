using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : Target, IPlayerContext
{
    public static PlayerController Instance;

    [Header("Refs")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator animator;
    [SerializeField] private InputAction playerControls;
    public Collider2D col;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    public Vector2 playerMoveDirection;

    [Header("Progression")]
    public int experience;
    public int currentLevel;
    public int maxLevel;
    public List<int> playerLevels;

    [Header("Weapons")]
    [SerializeField] private WeaponInventory weaponInventory;

    [Header("Damage / Invulnerability")]
    [Tooltip("0 = permanent")]
    [SerializeField] private float invulnerabilityDuration = 0.5f;
    private float invulnerabilityTimer;

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

    public override void Heal(float amount)
    {
        base.Heal(amount);
        UIController.Instance.UpdateHealthSlider();
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

    // =========================================

    protected override void Awake()
    {
        base.Awake();

        col = GetComponent<Collider2D>();

        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }

    private void Start()
    {
        // Fill levels list up to maxLevel if needed
        for (int i = playerLevels.Count; i < maxLevel; i++)
            playerLevels.Add(Mathf.CeilToInt(playerLevels[playerLevels.Count - 1] * 1.1f + 15));

        // Health is initialised in Health.Awake() → CurrentHealth = MaxHealth
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
        // Input
        float inputX = Input.GetAxisRaw("Horizontal");
        float inputY = Input.GetAxisRaw("Vertical");
        playerMoveDirection = playerControls.ReadValue<Vector2>();

        // Anim
        if (playerMoveDirection == Vector2.zero)
        {
            animator.SetBool("moving", false);
        }
        else if (Time.timeScale != 0f)
        {
            animator.SetBool("moving", true);
            animator.SetFloat("moveX", inputX);
            animator.SetFloat("moveY", inputY);
        }

        HandleWeaponSwitching();

        // Invulnerability countdown (maps to Health.IsInvulnerable)
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

    private void HandleWeaponSwitching()
    {
        if (!weaponInventory) return;

        if (Input.GetKeyDown(KeyCode.Q))
            weaponInventory.Cycle(Hand.Left, +1);

        if (Input.GetKeyDown(KeyCode.E))
            weaponInventory.Cycle(Hand.Right, +1);
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
        AudioController.Instance.PlaySound(AudioController.Instance.selectUpgrade);
    }

    public void IncreaseMovementSpeed(float multiplier)
    {
        moveSpeed *= multiplier;
        UIController.Instance.LevelUpPanelClose();
        AudioController.Instance.PlaySound(AudioController.Instance.selectUpgrade);
    }

    private void OnEnable() => playerControls.Enable();
    private void OnDisable() => playerControls.Disable();
}