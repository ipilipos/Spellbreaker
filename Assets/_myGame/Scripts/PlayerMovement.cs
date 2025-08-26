using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 6f; // Only one speed needed
    [SerializeField] private float acceleration = 10f;
    [SerializeField] private float deceleration = 15f;

    [Header("Input Settings")]
    [SerializeField] private bool enableController = true;
    [SerializeField] private float deadZone = 0.1f;

    // Components
    private Rigidbody2D rb;
    private CharacterAnimator characterAnimator;
    private SpriteRenderer spriteRenderer;

    // Movement variables
    private Vector2 moveInput;
    private bool facingRight = true;

    // Animation states
    private bool isMoving;
    private string currentAnimation = "";

    void Start()
    {
        // Get required components
        rb = GetComponent<Rigidbody2D>();
        characterAnimator = GetComponent<CharacterAnimator>();

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
    }

    void Update()
    {
        HandleInput();
        // No ground checking needed for no-gravity game
        HandleAnimations();
    }

    void FixedUpdate()
    {
        HandleMovement();
        // No jump handling needed for no-gravity game
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
        // Always use full moveSpeed - no walking speed
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

        // Smooth movement using acceleration/deceleration
        Vector2 velocityDiff = targetVelocity - rb.linearVelocity;
        float accelRate = (targetVelocity.magnitude > 0.01f) ? acceleration : deceleration;

        // Apply movement force
        Vector2 movement = velocityDiff * accelRate;
        rb.AddForce(movement, ForceMode2D.Force);

        // Update movement state
        isMoving = rb.linearVelocity.magnitude > 0.1f;
    }

   


    void HandleAnimations()
    {
        if (characterAnimator == null) return;

        string targetAnimation = "";

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

    void OnLanded()
    {
        // Not needed for bullet hell game - removing
    }

    void CreateGroundCheck()
    {
        // Not needed for bullet hell game - removing
    }

    // Public methods for external scripts
    public bool IsMoving() => isMoving;
    public float GetMoveInput() => moveInput.magnitude; // Changed to magnitude for 2D movement
    public Vector2 GetVelocity() => rb.linearVelocity;

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

    // Gizmos for debugging (bullet hell game - show movement only)
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
    }
}