using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Spine.Unity;
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private bool instantStop = true; // For bullet hell precise movement

    [Header("Input Settings")]
    [SerializeField] private bool enableController = true;
    [SerializeField] private float deadZone = 0.1f;

    [Header("Attack Settings")]
    [SerializeField] private float attackRange = 3f;
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private LayerMask enemyLayerMask = -1; // Which layers contain enemies
    [SerializeField] private bool autoAttack = true; // Automatically attack when in range
    [SerializeField] private bool manualAttackInput = true; // Allow manual attacks with input

    [Header("Attack Input")]
    [SerializeField] private KeyCode attackKey = KeyCode.Space;
    [SerializeField] private string attackButton = "Fire1"; // Left mouse button by default

    [Header("Visual Feedback")]
    [SerializeField] private bool showAttackRange = true;
    [SerializeField] private Color attackRangeColor = Color.blue;
    [SerializeField] private bool showAttackEffect = true;
    [SerializeField] private float attackEffectDuration = 0.2f;

    [Header("Audio")]
    [SerializeField] private AudioClip attackSound;
    [SerializeField] private AudioClip hitSound;

    // Components
    private Rigidbody2D rb;
    private CharacterAnimator characterAnimator;
    private SpriteRenderer spriteRenderer;
    private PlayerStats playerStats;
    private AudioSource audioSource;

    // Movement variables
    private Vector2 moveInput;
    private bool facingRight = true;

    // Animation states
    private bool isMoving;
    private string currentAnimation = "";

    // Attack variables
    private bool canAttack = true;
    private bool isAttacking = false;
    private List<EnemyMovement> enemiesInRange = new List<EnemyMovement>();
    private EnemyMovement currentTarget;
    private LineRenderer attackEffect;
    [Header("Damage Flash Settings")]
    [SerializeField] private bool enableDamageFlash = true;
    [SerializeField] private Color damageFlashColor = Color.red;
    [SerializeField] private float flashDuration = 0.15f;

    // Add these private variables
    private SkeletonAnimation skeletonAnimation;
    private bool isFlashing = false;
    private Color originalColor = Color.white;

    // Add this to your Start() method
    // Get Spine skeleton animation from child CharacterGFX
    [SerializeField] Transform characterGFX;
    void Start()
    {
        // Get required components
        rb = GetComponent<Rigidbody2D>();
        characterAnimator = GetComponent<CharacterAnimator>();
        playerStats = GetComponent<PlayerStats>();

        // Get sprite renderer from child CharacterGFX
        Transform characterGFX = transform.Find("CharacterGFX");
        if (characterGFX != null)
        {
            spriteRenderer = characterGFX.GetComponent<SpriteRenderer>();
        }

        // Ensure we have a Rigidbody2D
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }

        // Configure Rigidbody2D (No Gravity Game)
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.gravityScale = 0f; // No gravity
        rb.linearDamping = 8f; // Add some drag to stop sliding
        rb.angularDamping = 5f;

        // Add BoxCollider2D if not present
        if (GetComponent<BoxCollider2D>() == null)
        {   
            BoxCollider2D col = gameObject.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1f, 2f);
        }

        // Add AudioSource if not present
        if (audioSource == null && (attackSound != null || hitSound != null))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        // Create attack effect (optional visual feedback)
        if (showAttackEffect)
        {
            CreateAttackEffect();
        }

        // Subscribe to player death to stop attacking
        if (playerStats != null)
        {
            playerStats.OnDeath += OnPlayerDeath;
        }

        if (characterGFX != null)
        {
            skeletonAnimation = characterGFX.GetComponent<SkeletonAnimation>();
            if (skeletonAnimation != null)
            {
                originalColor = skeletonAnimation.skeleton.GetColor();
            }
        }

        if (playerStats != null)
        {
            playerStats.OnDamageTaken += OnPlayerDamageTaken;
        }
    }
    void OnPlayerDamageTaken(int damage)
    {
        if (enableDamageFlash && !isFlashing)
        {
            StartCoroutine(FlashSprite());
        }
    }

    IEnumerator FlashSprite()
    {
        if (skeletonAnimation == null) yield break;

        isFlashing = true;

        // Store original color
        Color originalSkeletonColor = skeletonAnimation.skeleton.GetColor();

        // Flash to damage color
        skeletonAnimation.skeleton.SetColor(damageFlashColor);

        yield return new WaitForSeconds(flashDuration);

        // Return to original color if player is not dead
        if (playerStats != null && !playerStats.IsDead())
        {
            skeletonAnimation.skeleton.SetColor(originalSkeletonColor);
        }

        isFlashing = false;
    }
    void Update()
    {
        HandleInput();
        HandleAnimations();

        // Attack system updates (only if player is alive)
        if (playerStats == null || !playerStats.IsDead())
        {
            DetectEnemiesInRange();
            HandleAttackInput();
            UpdateTargeting();

            // Auto attack if enabled and we have a target
            if (autoAttack && currentTarget != null && canAttack && !isAttacking)
            {
                StartAttack();
            }
        }
    }

    void FixedUpdate()
    {
        HandleMovement();
    }

    void HandleInput()
    {
        // Keyboard input (WASD + Arrow Keys)
        float horizontal = 0f;
        float vertical = 0f;

        // Horizontal input
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            horizontal -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            horizontal += 1f;

        // Vertical input (for 4-directional movement)
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            vertical += 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            vertical -= 1f;

        // Controller input (if enabled)
        if (enableController)
        {
            float controllerHorizontal = Input.GetAxis("Horizontal");
            float controllerVertical = Input.GetAxis("Vertical");

            // Apply deadzone
            if (Mathf.Abs(controllerHorizontal) > deadZone)
                horizontal += controllerHorizontal;
            if (Mathf.Abs(controllerVertical) > deadZone)
                vertical += controllerVertical;
        }

        // Clamp values
        horizontal = Mathf.Clamp(horizontal, -1f, 1f);
        vertical = Mathf.Clamp(vertical, -1f, 1f);

        moveInput = new Vector2(horizontal, vertical);
    }

    void HandleMovement()
    {
        Vector2 targetVelocity = Vector2.zero;

        // Calculate target velocity for both X and Y movement
        if (Mathf.Abs(moveInput.x) > 0.1f)
        {
            targetVelocity.x = moveSpeed * moveInput.x;

            // Handle sprite flipping
            if (moveInput.x > 0 && !facingRight)
                Flip();
            else if (moveInput.x < 0 && facingRight)
                Flip();
        }

        if (Mathf.Abs(moveInput.y) > 0.1f)
        {
            targetVelocity.y = moveSpeed * moveInput.y;
        }

        if (instantStop)
        {
            // Instant movement - perfect for bullet hell games
            rb.linearVelocity = targetVelocity;
        }
        else
        {
            // Smooth movement (if you want to toggle back)
            Vector2 velocityDiff = targetVelocity - rb.linearVelocity;
            float accelRate = (targetVelocity.magnitude > 0.01f) ? 20f : 30f;
            Vector2 movement = velocityDiff * accelRate;
            rb.AddForce(movement, ForceMode2D.Force);
        }

        // Update movement state
        isMoving = rb.linearVelocity.magnitude > 0.1f;
    }

    void HandleAnimations()
    {
        if (characterAnimator == null) return;

        string targetAnimation = "";

        // Check if we're attacking first
        if (isAttacking)
        {
            // Don't change animation while attacking
            return;
        }

        // Simple animation logic - only Run when moving, Idle when not
        if (isMoving)
        {
            targetAnimation = "Run"; // Always use Run animation when moving
        }
        else
        {
            targetAnimation = "Idle";
        }

        // Only change animation if it's different from current
        if (targetAnimation != currentAnimation)
        {
            characterAnimator.ChangeAnimation(targetAnimation);
            currentAnimation = targetAnimation;
        }
    }

    // ATTACK SYSTEM METHODS

    void DetectEnemiesInRange()
    {
        enemiesInRange.Clear();

        // Find all enemies in attack range
        Collider2D[] enemyColliders = Physics2D.OverlapCircleAll(transform.position, attackRange, enemyLayerMask);

        foreach (Collider2D enemyCollider in enemyColliders)
        {
            EnemyMovement enemy = enemyCollider.GetComponent<EnemyMovement>();
            EnemyStats enemyStats = enemyCollider.GetComponent<EnemyStats>();

            // Only add living enemies
            if (enemy != null && enemyStats != null && !enemyStats.IsDead())
            {
                enemiesInRange.Add(enemy);
            }
        }
    }

    void UpdateTargeting()
    {
        // Find the closest enemy as our target
        currentTarget = null;
        float closestDistance = float.MaxValue;

        foreach (EnemyMovement enemy in enemiesInRange)
        {
            if (enemy == null) continue;

            float distance = Vector2.Distance(transform.position, enemy.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                currentTarget = enemy;
            }
        }
    }

    void HandleAttackInput()
    {
        if (!manualAttackInput) return;

        bool attackInput = Input.GetKeyDown(attackKey) || Input.GetButtonDown(attackButton);

        if (attackInput && canAttack && !isAttacking && currentTarget != null)
        {
            StartAttack();
        }
    }

    void StartAttack()
    {
        if (!canAttack || isAttacking || currentTarget == null) return;

        StartCoroutine(PerformAttack());
    }

    IEnumerator PerformAttack()
    {
        isAttacking = true;
        canAttack = false;

        // Randomly select one of the three Duelist attack animations
        if (characterAnimator != null)
        {
            string[] duelistAttacks = { "Attack1", "Attack2", "Special" }; // Special maps to "Attack 3 DUELIST"
            string selectedAttack = duelistAttacks[Random.Range(0, duelistAttacks.Length)];

            characterAnimator.ChangeAnimation(selectedAttack);
            currentAnimation = selectedAttack; // Update current animation tracker
        }

        // Play attack sound
        if (attackSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(attackSound);
        }

        // Show attack effect
        if (showAttackEffect && currentTarget != null)
        {
            ShowAttackEffect(currentTarget.transform.position);
        }

        // Wait for a brief moment (attack windup)
        yield return new WaitForSeconds(0.3f);

        // Deal damage if target is still valid and in range
        if (currentTarget != null)
        {
            float distanceToTarget = Vector2.Distance(transform.position, currentTarget.transform.position);

            if (distanceToTarget <= attackRange)
            {
                EnemyStats enemyStats = currentTarget.GetComponent<EnemyStats>();
                if (enemyStats != null && !enemyStats.IsDead())
                {
                    // Deal damage using player's damage stat
                    int damageAmount = playerStats != null ? playerStats.GetDamage() : 30;
                    enemyStats.TakeDamage(damageAmount);

                    // Play hit sound
                    if (hitSound != null && audioSource != null)
                    {
                        audioSource.PlayOneShot(hitSound);
                    }
                }
            }
        }

        // Wait for rest of attack duration
        yield return new WaitForSeconds(0.2f);

        isAttacking = false;

        // Wait for cooldown
        yield return new WaitForSeconds(attackCooldown);

        canAttack = true;
    }

    void CreateAttackEffect()
    {
        // Create a simple line renderer for attack effect
        GameObject effectObj = new GameObject("AttackEffect");
        effectObj.transform.SetParent(transform);
        effectObj.transform.localPosition = Vector3.zero;

        attackEffect = effectObj.AddComponent<LineRenderer>();

        // Create material and set color
        Material attackMaterial = new Material(Shader.Find("Sprites/Default"));
        attackMaterial.color = Color.white;
        attackEffect.material = attackMaterial;

        attackEffect.startWidth = 0.1f;
        attackEffect.endWidth = 0.05f;
        attackEffect.positionCount = 2;
        attackEffect.enabled = false;
        attackEffect.sortingOrder = 100;
    }

    void ShowAttackEffect(Vector3 targetPosition)
    {
        if (attackEffect != null)
        {
            StartCoroutine(AttackEffectCoroutine(targetPosition));
        }
    }

    IEnumerator AttackEffectCoroutine(Vector3 targetPosition)
    {
        if (attackEffect == null) yield break;

        attackEffect.enabled = true;
        attackEffect.SetPosition(0, transform.position);
        attackEffect.SetPosition(1, targetPosition);

        yield return new WaitForSeconds(attackEffectDuration);

        attackEffect.enabled = false;
    }

    void OnPlayerDeath()
    {
        // Stop attacking when player dies
        StopAllCoroutines();
        isAttacking = false;
        canAttack = false;

        if (attackEffect != null)
        {
            attackEffect.enabled = false;
        }
    }

    void Flip()
    {
        facingRight = !facingRight;

        // Method 1: Flip using scale (works with Spine)
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;

        // Method 2: Alternative - flip using SpriteRenderer if needed
        // if (spriteRenderer != null)
        // {
        //     spriteRenderer.flipX = !facingRight;
        // }
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        if (playerStats != null)
        {
            playerStats.OnDeath -= OnPlayerDeath;
            playerStats.OnDamageTaken -= OnPlayerDamageTaken;
        }
    }

    // Public methods for external scripts
    public bool IsMoving() => isMoving;
    public float GetMoveInput() => moveInput.magnitude; // Changed to magnitude for 2D movement
    public Vector2 GetVelocity() => rb.linearVelocity;

    // Attack system public methods
    public bool IsAttacking() => isAttacking;
    public bool CanAttack() => canAttack;
    public EnemyMovement GetCurrentTarget() => currentTarget;
    public List<EnemyMovement> GetEnemiesInRange() => new List<EnemyMovement>(enemiesInRange);
    public float GetAttackRange() => attackRange;

    // Public setters for upgrades/modifications
    public void SetAttackRange(float newRange) => attackRange = newRange;
    public void SetAttackCooldown(float newCooldown) => attackCooldown = newCooldown;
    public void SetAutoAttack(bool enabled) => autoAttack = enabled;

    // Method to temporarily disable movement (for cutscenes, etc.)
    public void SetMovementEnabled(bool enabled)
    {
        this.enabled = enabled;
        if (!enabled)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            moveInput = Vector2.zero;
        }
    }

    // Method to add external forces (knockback, etc.)
    public void AddForce(Vector2 force, ForceMode2D mode = ForceMode2D.Impulse)
    {
        rb.AddForce(force, mode);
    }

    // Force attack method (for special abilities)
    public void ForceAttack()
    {
        if (currentTarget != null && !isAttacking)
        {
            StartAttack();
        }
    }

    // Attack all enemies in range (for area attacks)
    public void AttackAllInRange()
    {
        if (!canAttack || isAttacking || playerStats == null || playerStats.IsDead())
            return;

        foreach (EnemyMovement enemy in enemiesInRange)
        {
            if (enemy != null)
            {
                EnemyStats enemyStats = enemy.GetComponent<EnemyStats>();
                if (enemyStats != null && !enemyStats.IsDead())
                {
                    enemyStats.TakeDamage(playerStats.GetDamage());
                }
            }
        }
    }

    // Gizmos for debugging
    void OnDrawGizmosSelected()
    {
        // Draw movement direction indicator
        if (Application.isPlaying && rb != null)
        {
            Gizmos.color = isMoving ? Color.green : Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.5f);

            if (isMoving)
            {
                Gizmos.color = Color.blue;
                Vector3 velocityDirection = rb.linearVelocity.normalized;
                Gizmos.DrawRay(transform.position, velocityDirection * 2f);
            }
        }

        // Draw attack range
        if (showAttackRange)
        {
            Gizmos.color = attackRangeColor;
            Gizmos.DrawWireSphere(transform.position, attackRange);

            // Draw line to current target
            if (Application.isPlaying && currentTarget != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, currentTarget.transform.position);
            }
        }
    }

    // Validation in editor
    void OnValidate()
    {
        if (attackRange < 0) attackRange = 0;
        if (attackCooldown < 0.1f) attackCooldown = 0.1f;
    }
}