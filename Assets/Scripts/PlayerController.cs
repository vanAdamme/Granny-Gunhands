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

    private void Awake()
    {
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
        // Fill playerLevels list to maxLevel
        for (int i = playerLevels.Count; i < maxLevel; i++)
        {
            playerLevels.Add(Mathf.CeilToInt(playerLevels[playerLevels.Count - 1] * 1.1f + 15));
        }

        // Health now comes from Health/Target base class:
        // MaxHealth (get/set) and CurrentHealth (get)
        // Ensure UI reflects starting values
        UIController.Instance.UpdateHealthSlider();
        UIController.Instance.UpdateExperienceSlider();
    }

    private void Update()
    {
        // Input (new Input System)
        playerMoveDirection = playerControls.ReadValue<Vector2>();

        if (playerMoveDirection == Vector2.zero)
        {
            animator.SetBool("moving", false);
        }
        else if (Time.timeScale != 0)
        {
            animator.SetBool("moving", true);

            // Old anim parameters used raw axes—keep that feel
            float inputX = Input.GetAxisRaw("Horizontal");
            float inputY = Input.GetAxisRaw("Vertical");
            animator.SetFloat("moveX", inputX);
            animator.SetFloat("moveY", inputY);
        }

        // Handle immunity countdown (maps to Health.IsInvulnerable)
        if (immunityTimer > 0f)
        {
            immunityTimer -= Time.deltaTime;
            if (immunityTimer <= 0f)
                SetImmune(false);
        }
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = playerMoveDirection * moveSpeed;
    }

    /// <summary>
    /// Override TakeDamage so we can apply brief immunity & update UI,
    /// then forward to base (Target → Health) to actually reduce HP.
    /// </summary>
    public override void TakeDamage(float amount)
    {
        // Respect current invulnerability (Immunity window)
        if (IsInvulnerable) return;

        SetImmune(true);

        base.TakeDamage(amount);          // applies damage + popup + death check
        UIController.Instance.UpdateHealthSlider();
    }

    /// <summary>
    /// Player-specific death behaviour.
    /// </summary>
    protected override void Die()
    {
        base.Die(); // sets dead flag & deactivates the GameObject
        GameManager.Instance.GameOver();
    }

    private void SetImmune(bool state)
    {
        IsInvulnerable = state;
        if (state)
        {
            immunityTimer = immunityDuration;

            // small visual alpha cue
            var sr = GetComponent<SpriteRenderer>();
            if (sr)
            {
                var c = sr.color;
                c.a = 0.8f;
                sr.color = c;
            }
        }
        else
        {
            immunityTimer = 0f;

            var sr = GetComponent<SpriteRenderer>();
            if (sr)
            {
                var c = sr.color;
                c.a = 1f;
                sr.color = c;
            }
        }
    }

    public void AddExperience(int experienceToGet)
    {
        experience += experienceToGet;
        UIController.Instance.UpdateExperienceSlider();
        // Level-up logic can remain commented or restored later
        // if (experience >= playerLevels[currentLevel - 1]) { ... }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Item"))
        {
            GetCoin();
            Destroy(other.gameObject);
        }
    }

    public void GetCoin()
    {
        coins++;
        // update coin UI if you have one
    }

    public bool isHurt()
    {
        // Kept for compatibility where it’s used elsewhere
        return CurrentHealth < MaxHealth;
    }

    public void Heal(int value)
    {
        // Use Health.Heal and then refresh UI
        base.Heal(value);
        UIController.Instance.UpdateHealthSlider();
    }

    public void IncreaseMaxHealth(int value)
    {
        MaxHealth += value;
        base.Heal(value); // top up by the same amount, effectively setting to new max
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