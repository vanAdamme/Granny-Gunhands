using UnityEngine;
using DamageNumbersPro;
using Pathfinding;
using UnityEditor.Timeline.Actions;

public class Enemy : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private Animator anim;
    private Collider2D col;
    private AIPath path;
    [SerializeField] private Transform target;

    [SerializeField] bool rangedAttack;
    [SerializeField] bool meleeAttack;
    [SerializeField] bool touchAttack;

    [SerializeField] private float moveSpeed;
    [SerializeField] private float damage;
    [SerializeField] private float health;
    [SerializeField] private int experienceToGive;
    [SerializeField] private float attackRange;
    [SerializeField] private float pushTime;
    [SerializeField] private float dropChance;
    [SerializeField] private GameObject itemDrop;
    [SerializeField] private AudioClip deathClip;

    private float pushCounter;
    private Vector3 direction;
    private bool isAttacking = false;

    [SerializeField] private DamageNumber numberPrefab;
    [SerializeField] private GameObject destroyEffect;

    private AudioSource audioSource;
    private DamageFlash damageFlash;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        damageFlash = GetComponent<DamageFlash>();
        // col = GetComponent<Collider2D>();
        path = GetComponent<AIPath>();
        target = GameObject.FindWithTag("Player").transform;
    }

    private void Update()
    {
        path.destination = target.position;
    }

    void FixedUpdate()
    {
        if (PlayerController.Instance.gameObject.activeSelf)
        {
            float distance = Vector2.Distance(transform.position, PlayerController.Instance.transform.position);

            if (meleeAttack && !isAttacking && distance <= attackRange)
            {
                // Start attack
                //Attack();
            }

            // face the player
            if (PlayerController.Instance.transform.position.x > transform.position.x)
            {
                spriteRenderer.flipX = true;
            }
            else
            {
                spriteRenderer.flipX = false;
            }

            // push back
            if (pushCounter > 0)
            {
                pushCounter -= Time.deltaTime;
                if (moveSpeed > 0)
                {
                    moveSpeed = -moveSpeed;
                }
                if (pushCounter <= 0)
                {
                    moveSpeed = Mathf.Abs(moveSpeed);
                }
            }
            // move towards the player
            direction = (PlayerController.Instance.transform.position - transform.position).normalized;
            rb.linearVelocity = new Vector2(direction.x * moveSpeed, direction.y * moveSpeed);
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("PlayerHitbox"))
        {
            PlayerController.Instance.TakeDamage(damage);
        }
    }

    public void OnAttackFinished()
    {
        isAttacking = false;
        anim.SetBool("isAttacking", false);
    }

    public void TakeDamage(float damage)
    {
        // take damage
        health -= damage;

        // damage popup
        DamageNumber damageNumber = numberPrefab.Spawn(transform.position, damage);

        // push enemy back
        pushCounter = pushTime;

        // dead?
        if (health <= 0)
        {
            Die();
        }

        // damage flash
        damageFlash.CallDamageFlash();
    }

    void Die()
    {
        DeathCroak();
        DropItem();
        Instantiate(destroyEffect, transform.position, transform.rotation);
        Destroy(gameObject);
        PlayerController.Instance.GetExperience(experienceToGive);
    }

    void DropItem()
    {
        if (Random.Range(0f, 1f) < dropChance)
        {
            GameObject drop = Instantiate(itemDrop, transform.position, transform.rotation);
        }
    }

    void DeathCroak()
    {
        // Create a temporary object just for the death sound
        GameObject soundObj = new GameObject("DeathCroak");
        AudioSource tempSource = soundObj.AddComponent<AudioSource>();
        tempSource.clip = deathClip;
        tempSource.Play();

        // Destroy the sound object after the clip ends
        Destroy(soundObj, deathClip.length);
    }
}