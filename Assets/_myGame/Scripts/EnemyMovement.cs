using System.Collections;
using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float stopDistance = 1.5f; // NEW: Distance to stop from player
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
    private PlayerStats playerStats; // NEW: Reference to player stats

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
        Dead,
        Idle // NEW: For when player is dead
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
            playerStats = playerObject.GetComponent<PlayerStats>(); // NEW: Get player stats
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

        // NEW: Subscribe to player death event
        if (playerStats != null)
        {
            playerStats.OnDeath += HandlePlayerDeath;
            playerStats.OnRevive += HandlePlayerRevive;
        }

        rb.freezeRotation = true;
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;

        // NEW: Ensure stop distance is less than attack range
        if (stopDistance >= attackRange)
        {
            stopDistance = attackRange * 0.8f;
            Debug.LogWarning($"Stop distance adjusted to {stopDistance} to be less than attack range");
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
        // NEW: Check if player is dead first
        if (playerStats != null && playerStats.IsDead())
        {
            if (currentState != EnemyState.Idle)
            {
                currentState = EnemyState.Idle;
                isAttacking = false; // Stop any current attack
            }
            return;
        }

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
                    currentState = EnemyState.Chasing;
                }
                // Add fallback - if too far away, go back to chasing
                else if (distanceToPlayer > attackRange * 1.5f)
                {
                    currentState = EnemyState.Chasing;
                    isAttacking = false;
                }
                break;

            case EnemyState.Idle:
                // Stay idle until player is revived
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
                if (!isAttacking && distanceToPlayer > stopDistance) // NEW: Only move if beyond stop distance
                {
                    if (rb.bodyType == RigidbodyType2D.Kinematic)
                    {
                        // Kinematic movement (no physics interactions)
                        Vector3 newPosition = transform.position + (Vector3)moveDirection * moveSpeed * Time.deltaTime;
                        transform.position = newPosition;
                        isMoving = true;
                    }
                    else
                    {
                        // Physics-based movement (original code)
                        targetVelocity = moveDirection * moveSpeed;
                        isMoving = true;
                    }

                    // Handle rotation to face player
                    if (canRotateToFacePlayer)
                    {
                        RotateTowardsPlayer();
                    }
                }
                else if (canRotateToFacePlayer)
                {
                    // NEW: Still face player even when not moving
                    RotateTowardsPlayer();
                }
                break;

            case EnemyState.Attacking:
                // Stop moving while attacking but still face the player
                if (canRotateToFacePlayer)
                {
                    RotateTowardsPlayer();
                }
                break;

            case EnemyState.Idle:
                // NEW: No movement when player is dead
                break;
        }

        // Apply movement (only for non-kinematic bodies)
        if (rb.bodyType != RigidbodyType2D.Kinematic)
        {
            if (smoothMovement)
            {
                rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, targetVelocity, Time.deltaTime * 8f);
            }
            else
            {
                rb.linearVelocity = targetVelocity;
            }
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
        // NEW: Don't attack if player is dead
        if (currentState == EnemyState.Attacking && canAttack && !isAttacking &&
            (playerStats == null || !playerStats.IsDead()))
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

        // Deal damage to player if still in range AND player is alive
        if (distanceToPlayer <= attackRange && playerStats != null && !playerStats.IsDead())
        {
            if (enemyStats != null)
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

        // NEW: Play idle when player is dead
        if (currentState == EnemyState.Idle)
        {
            enemyAnimator.PlayAnimation("Idle");
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

    // NEW: Handle when player dies
    void HandlePlayerDeath()
    {
        currentState = EnemyState.Idle;
        isAttacking = false;

        // Stop any ongoing attack coroutines
        StopAllCoroutines();

        // Reset attack state
        canAttack = true;
    }

    // NEW: Handle when player is revived
    void HandlePlayerRevive()
    {
        if (currentState == EnemyState.Idle && enemyStats != null && !enemyStats.IsDead())
        {
            currentState = EnemyState.Chasing;
        }
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
    public void SetStopDistance(float newStopDistance) // NEW: Method to adjust stop distance
    {
        stopDistance = newStopDistance;
        // Ensure stop distance is less than attack range
        if (stopDistance >= attackRange)
        {
            stopDistance = attackRange * 0.8f;
        }
    }

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

        // NEW: Unsubscribe from player events
        if (playerStats != null)
        {
            playerStats.OnDeath -= HandlePlayerDeath;
            playerStats.OnRevive -= HandlePlayerRevive;
        }
    }

    // Debug visualization
    void OnDrawGizmosSelected()
    {
        // Draw attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // NEW: Draw stop distance
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, stopDistance);

        // Draw line to player if in game
        if (Application.isPlaying && player != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, player.position);
        }
    }
}