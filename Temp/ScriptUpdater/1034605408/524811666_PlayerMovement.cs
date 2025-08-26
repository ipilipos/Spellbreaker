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

        // Configure Rigidbody2D
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.gravityScale = 2f;

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
        CheckGrounded();
        HandleAnimations();
    }

    void FixedUpdate()
    {
        HandleMovement();
        HandleJump();
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

        // Vertical input (for jump)
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.Space))
            vertical = 1f;

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
        vertical = Mathf.Clamp01(vertical);

        moveInput = new Vector2(horizontal, vertical);

        // Jump input
        jumpPressed = Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow);
        jumpHeld = Input.GetButton("Jump") || Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);

        // Run input (Left Shift or Right Bumper on controller)
        isRunning = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ||
                   Input.GetButton("Fire1") || Input.GetAxis("Fire1") > 0.5f;
    }

    void HandleMovement()
    {
        float targetSpeed = 0f;

        if (Mathf.Abs(moveInput.x) > 0.1f)
        {
            // Determine target speed based on running state
            targetSpeed = isRunning ? runSpeed : walkSpeed;
            targetSpeed *= Mathf.Sign(moveInput.x);

            // Handle sprite flipping
            if (moveInput.x > 0 && !facingRight)
                Flip();
            else if (moveInput.x < 0 && facingRight)
                Flip();
        }

        // Smooth movement using acceleration/deceleration
        float speedDifference = targetSpeed - rb.linearVelocity.x;
        float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : deceleration;

        // Apply movement force
        float movement = speedDifference * accelRate;
        rb.AddForce(movement * Vector2.right, ForceMode2D.Force);

        // Update movement state
        isMoving = Mathf.Abs(rb.linearVelocity.x) > 0.1f;
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

        // Determine animation state based on current job and movement
        if (!isGrounded)
        {
            // In air - use jump animations
            if (rb.linearVelocity.y > 0.5f)
                targetAnimation = "Jump1";
            else if (rb.linearVelocity.y < -0.5f)
                targetAnimation = "Jump3";
            else
                targetAnimation = "Jump2";
        }
        else if (isMoving)
        {
            // Moving on ground
            if (isRunning && Mathf.Abs(rb.linearVelocity.x) > walkSpeed * 1.2f)
                targetAnimation = "Run";
            else
                targetAnimation = "Walk";
        }
        else
        {
            // Idle on ground
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