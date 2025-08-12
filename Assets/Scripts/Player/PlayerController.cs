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
    public int coins;
    public List<int> playerLevels;

    [Header("Weapons")]
    [SerializeField] private List<Weapon> inactiveWeapons;
    public List<Weapon> activeWeapons;
    [SerializeField] private List<Weapon> upgradeableWeapons;
    public List<Weapon> maxLevelWeapons;

    [Header("Damage / Immunity")]
    [Tooltip("0 = permanent")]
    [SerializeField] private float immunityDuration = 0.5f;
    private float immunityTimer;

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

    public Transform Transform => transform;

    public void AddWeapon(Weapon weaponPrefab)
    {
        if (!weaponPrefab) return;

        // Instantiate and parent under player (so pooled/scene hierarchy stays tidy)
        var w = Instantiate(weaponPrefab, transform);
        // Track in active list if you’re using it elsewhere
        if (activeWeapons == null) activeWeapons = new List<Weapon>();
        activeWeapons.Add(w);

        // Optional: hook up UI icon if you have left/right slots etc.
        // UIController.Instance.UpdateLeftWeaponIcon(w.weaponImage);
    }

    public bool TryGetActiveWeapon<T>(out T weapon) where T : MonoBehaviour
    {
        // 1) look through the activeWeapons list
        if (activeWeapons != null)
        {
            foreach (var w in activeWeapons)
            {
                if (w == null) continue;
                weapon = w.GetComponent<T>();
                if (weapon != null) return true;
            }
        }
        // 2) fallback: search children (in case you spawn elsewhere)
        weapon = GetComponentInChildren<T>();
        return weapon != null;
    }
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
        if (immunityDuration > 0f) SetImmune(true);
    }

    protected override void Die()
    {
        base.Die();
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