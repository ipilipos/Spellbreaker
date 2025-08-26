using System.Collections;
using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float rotationSpeed = 360f;
    [SerializeField] private bool smoothMovement = true;

    [Header("Attack Settings")]
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private float attackDuration = 1f;

    [Header("AI Settings")]
    [SerializeField] private bool canRotateToFacePlayer = true;

    // Components
    private Rigidbody2D rb;
    private EnemyStats enemyStats;
    private EnemyAnimator enemyAnimator;
    private Transform player;

    // State variables
    private bool canAttack = true;
    private bool isAttacking = false;
    private bool isMoving = false;
    private float distanceToPlayer;
    private Vector2 moveDirection;

    // Enemy states
    public enum EnemyState
    {
        Chasing,
        Attacking,
        Dead
    }

    private EnemyState currentState = EnemyState.Chasing;

    void Start()
    {
        // Get required components
        rb = GetComponent<Rigidbody2D>();
        enemyStats = GetComponent<EnemyStats>();
        enemyAnimator = GetComponent<EnemyAnimator>();

        // Find player
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject == null)
        {
            // Fallback: find by name if no tag
            playerObject = GameObject.Find("Lightbreaker");
        }

        if (playerObject != null)
        {
            player = playerObject.transform;
        }
        else
        {
            Debug.LogWarning("Player not found! Make sure player has 'Player' tag or is named 'Lightbreaker'");
        }

        // Ensure we have a Rigidbody2D
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }

        // Configure Rigidbody2D for bullet hell movement
        rb.freezeRotation = true;
        rb.gravityScale = 0f;
        rb.linearDamping = 5f;
        rb.angularDamping = 5f;

        // Subscribe to enemy death event
        if (enemyStats != null)
        {
            enemyStats.OnDeath += HandleDeath;
        }
    }

    void Update()
    {
        if (currentState == EnemyState.Dead || player == null || enemyStats == null || enemyStats.IsDead())
            return;

        UpdatePlayerDistance();
        UpdateState();
        HandleMovement();
        HandleAttack();
    }

    void UpdatePlayerDistance()
    {
        if (player != null)
        {
            distanceToPlayer = Vector2.Distance(transform.position, player.position);
            moveDirection = (player.position - transform.position).normalized;
        }
    }

    void UpdateState()
    {
        switch (currentState)
        {
            case EnemyState.Chasing:
                if (distanceToPlayer <= attackRange && canAttack && !isAttacking)
                {
                    currentState = EnemyState.Attacking;
                }
                break;

            case EnemyState.Attacking:
                if (!isAttacking)
                {
                    // Return to chasing after attack is complete
                    currentState = EnemyState.Chasing;
                }
                break;
        }
    }

    void HandleMovement()
    {
        Vector2 targetVelocity = Vector2.zero;
        isMoving = false;

        switch (currentState)
        {
            case EnemyState.Chasing:
                if (!isAttacking)
                {
                    // Always move towards player when chasing
                    targetVelocity = moveDirection * moveSpeed;
                    isMoving = true;

                    // Handle rotation to face player
                    if (canRotateToFacePlayer)
                    {
                        RotateTowardsPlayer();
                    }
                }
                break;

            case EnemyState.Attacking:
                // Stop moving while attacking but still face the player
                if (canRotateToFacePlayer)
                {
                    RotateTowardsPlayer();
                }
                break;
        }

        // Apply movement
        if (smoothMovement)
        {
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, targetVelocity, Time.deltaTime * 8f);
        }
        else
        {
            rb.linearVelocity = targetVelocity;
        }

        // Update animations based on movement
        UpdateAnimations();
    }

    void RotateTowardsPlayer()
    {
        if (moveDirection.x > 0.1f && transform.localScale.x < 0)
        {
            // Face right
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
        else if (moveDirection.x < -0.1f && transform.localScale.x > 0)
        {
            // Face left
            Vector3 scale = transform.localScale;
            scale.x = -Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
    }

    void HandleAttack()
    {
        if (currentState == EnemyState.Attacking && canAttack && !isAttacking)
        {
            StartCoroutine(PerformAttack());
        }
    }

    IEnumerator PerformAttack()
    {
        isAttacking = true;
        canAttack = false;

        // Play attack animation
        if (enemyAnimator != null)
        {
            enemyAnimator.PlayAnimation("Attack");
        }

        // Wait for attack duration
        yield return new WaitForSeconds(attackDuration);

        // Deal damage to player if still in range
        if (distanceToPlayer <= attackRange)
        {
            PlayerStats playerStats = player.GetComponent<PlayerStats>();
            if (playerStats != null && enemyStats != null)
            {
                playerStats.TakeDamage(enemyStats.GetDamage());
            }
        }

        isAttacking = false;

        // Wait for cooldown
        yield return new WaitForSeconds(attackCooldown);

        canAttack = true;
    }

    void UpdateAnimations()
    {
        if (enemyAnimator == null) return;

        if (currentState == EnemyState.Dead)
        {
            // Death animation is handled in HandleDeath
            return;
        }

        if (isAttacking)
        {
            // Attack animation is already playing
            return;
        }

        if (isMoving)
        {
            enemyAnimator.PlayAnimation("Walk");
        }
        else
        {
            enemyAnimator.PlayAnimation("Idle");
        }
    }

    void HandleDeath()
    {
        currentState = EnemyState.Dead;

        // Stop all movement
        rb.linearVelocity = Vector2.zero;
        isMoving = false;
        isAttacking = false;

        // Play death animation
        if (enemyAnimator != null)
        {
            enemyAnimator.PlayAnimation("Dead");
        }

        // Disable collider and movement
        if (GetComponent<Collider2D>() != null)
        {
            GetComponent<Collider2D>().enabled = false;
        }

        this.enabled = false;

        // Optionally destroy after delay
        StartCoroutine(DestroyAfterDelay(3f));
    }

    IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }

    // Public getters for other scripts
    public EnemyState GetCurrentState() => currentState;
    public bool IsInAttackRange() => distanceToPlayer <= attackRange;
    public float GetDistanceToPlayer() => distanceToPlayer;

    // Public methods for external control
    public void SetMoveSpeed(float newSpeed) => moveSpeed = newSpeed;
    public void SetAttackRange(float newRange) => attackRange = newRange;

    // Damage the enemy (called by player attacks)
    public void TakeDamage(int damage)
    {
        if (enemyStats != null && currentState != EnemyState.Dead)
        {
            enemyStats.TakeDamage(damage);
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        if (enemyStats != null)
        {
            enemyStats.OnDeath -= HandleDeath;
        }
    }

    // Debug visualization
    void OnDrawGizmosSelected()
    {
        // Draw attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Draw line to player if in game
        if (Application.isPlaying && player != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, player.position);
        }
    }
}