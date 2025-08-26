using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 3f;
    [SerializeField] private float runSpeed = 6f;
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private float acceleration = 10f;
    [SerializeField] private float deceleration = 15f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayerMask = 1;

    [Header("Input Settings")]
    [SerializeField] private bool enableController = true;
    [SerializeField] private float deadZone = 0.1f;

    // Components
    private Rigidbody2D rb;
    private CharacterAnimator characterAnimator;
    private SpriteRenderer spriteRenderer;

    // Movement variables
    private Vector2 moveInput;
    private Vector2 currentVelocity;
    private bool isGrounded;
    private bool isRunning;
    private bool facingRight = true;

    // Input variables
    private bool jumpPressed;
    private bool jumpHeld;

    // Animation states
    private bool isMoving;
    private bool wasGrounded;
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

        // Create ground check if it doesn't exist
        if (groundCheck == null)
        {
            CreateGroundCheck();
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

        // Run input (Left Shift or Right Bumper on controller)
        isRunning = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ||
                   Input.GetButton("Fire1") || Input.GetAxis("Fire1") > 0.5f;
    }

    void HandleMovement()
    {
        Vector2 targetVelocity = Vector2.zero;

        // Calculate target velocity for both X and Y movement
        if (Mathf.Abs(moveInput.x) > 0.1f)
        {
            float targetSpeed = isRunning ? runSpeed : walkSpeed;
            targetVelocity.x = targetSpeed * moveInput.x;

            // Handle sprite flipping
            if (moveInput.x > 0 && !facingRight)
                Flip();
            else if (moveInput.x < 0 && facingRight)
                Flip();
        }

        if (Mathf.Abs(moveInput.y) > 0.1f)
        {
            float targetSpeed = isRunning ? runSpeed : walkSpeed;
            targetVelocity.y = targetSpeed * moveInput.y;
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

    void HandleJump()
    {
        if (jumpPressed && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }

        // Variable jump height
        if (!jumpHeld && rb.linearVelocity.y > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);
        }
    }

    void CheckGrounded()
    {
        wasGrounded = isGrounded;
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayerMask);

        // Landing detection
        if (!wasGrounded && isGrounded)
        {
            // Just landed
            OnLanded();
        }
    }

    void HandleAnimations()
    {
        if (characterAnimator == null) return;

        string targetAnimation = "";

        // For no-gravity game, focus on movement direction and speed
        if (isMoving)
        {
            if (isRunning && rb.linearVelocity.magnitude > walkSpeed * 1.2f)
                targetAnimation = "Run";
            else
                targetAnimation = "Walk";
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
        // Add any landing effects here
        // For example, dust particles, sound effects, etc.
    }

    void CreateGroundCheck()
    {
        GameObject groundCheckObj = new GameObject("GroundCheck");
        groundCheckObj.transform.SetParent(transform);
        groundCheckObj.transform.localPosition = new Vector3(0, -1f, 0);
        groundCheck = groundCheckObj.transform;
    }

    // Public methods for external scripts
    public bool IsGrounded() => isGrounded;
    public bool IsMoving() => isMoving;
    public bool IsRunning() => isRunning;
    public float GetMoveInput() => moveInput.x;
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

    // Gizmos for debugging
    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}