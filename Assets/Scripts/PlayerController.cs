using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : Target
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
    public Vector2 lastMoveDirection = new Vector2(0, -1);

    [Header("Progression")]
    public int experience;
    public int currentLevel;
    public int maxLevel;
    public int coins;
    public List<int> playerLevels;

    [Header("Weapons")]
    [SerializeField] private List<Weapon> inactiveWeapons;
    public List<Weapon> activeWeapons;
    [SerializeField] private List<Weapon> upgradeableWeapons;
    public List<Weapon> maxLevelWeapons;

    [Header("Damage / Immunity")]
    [SerializeField] private float immunityDuration = 0.5f;
    private float immunityTimer;

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
            lastMoveDirection = playerMoveDirection;
        }

        // Immunity countdown (maps to Health.IsInvulnerable)
        if (immunityTimer > 0f)
        {
            immunityTimer -= Time.deltaTime;
            if (immunityTimer <= 0f) SetImmune(false);
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
        SetImmune(true);
    }

    protected override void Die()
    {
        base.Die();                           // marks dead + sets inactive
        GameManager.Instance.GameOver();
    }

    private void SetImmune(bool state)
    {
        IsInvulnerable = state;
        var sr = GetComponent<SpriteRenderer>();
        if (state)
        {
            immunityTimer = immunityDuration;
            // if (sr)
            // {
            //     var c = sr.color; c.a = 0.8f; sr.color = c;
            // }
        }
        else
        {
            immunityTimer = 0f;
            // if (sr)
            // {
            //     var c = sr.color; c.a = 1f; sr.color = c;
            // }
        }
    }

    public void AddExperience(int amount)
    {
        experience += amount;
        UIController.Instance.UpdateExperienceSlider();
        // if (experience >= playerLevels[currentLevel - 1]) { ... level-up flow ... }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Item"))
        {
            GetCoin();
            Destroy(other.gameObject);
        }
    }

    public void GetCoin() => coins++;

    // Compatibility helpers that UI/other systems might still call
    public bool isHurt() => CurrentHealth < MaxHealth;

    public void Heal(int value)
    {
        base.Heal(value);
        UIController.Instance.UpdateHealthSlider();
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

    private void OnEnable()  => playerControls.Enable();
    private void OnDisable() => playerControls.Disable();
}