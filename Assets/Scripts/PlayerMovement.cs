using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public Rigidbody2D rb;

    [Header("Movement")]
    public float speed = 5f;
    float horizontalMovement = 0f;

    // Expose the last input horizontally so other systems can read it safely
    public float HorizontalInput { get; private set; } = 0f;

    [Header("Input")]
    [Tooltip("Deadzone below which stick/axis input is treated as no input (zero).")]
    public float inputDeadzone = 0.1f;

    [Header("Velocity Safety")]
    [Tooltip("Clamp magnitude of any velocity assigned to the Rigidbody to avoid runaway values.")]
    public float maxVelocityMagnitude = 100f;

    [Header("Jumping")]
    public float jumpForce = 10f;
    public int maxJump = 1;
    int jumpRemaining;

    // Reference to animation driver so we can trigger jump animation immediately
    public PlayerAnimation playerAnimation;

    // Coyote time + jump buffer
    [Header("Jump Assist")]
    [Tooltip("How long after leaving ground the player can still jump")]
    public float coyoteTime = 0.1f;
    [Tooltip("How long a jump input is buffered before landing")]
    public float jumpBufferTime = 0.1f;
    private float coyoteTimer = 0f;
    private float jumpBufferTimer = 0f;

    [Header("Ground Check")]
    public Transform groundCheckPos;
    public Vector2 groundCheckSize = new Vector2(0.5f, 0.5f);
    public LayerMask groundLayer;

    [Header("Gravity")]
    public float baseGravity = 2f;
    public float maxFallSpeed = -38f;
    public float fallSpeedMultiplier = 2f;

    // Facing direction: 1 = right, -1 = left
    public int facingDirection { get; private set; } = 1;

    // Expose grounded state so other systems can check (e.g. attacks)
    public bool IsGrounded { get; private set; } = false;

    // Prevent movement code from immediately overwriting external velocity changes (e.g. attack boost)
    private float velocityLockTimer = 0f;

    // Track previous grounded state to only refill jumps on landing
    private bool wasGrounded = false;

    void Awake()
    {
        // ensure we start with the configured number of jumps
        jumpRemaining = Mathf.Max(1, maxJump);
        if (rb != null) rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        // auto-find animation driver if not assigned
        if (playerAnimation == null)
            playerAnimation = GetComponent<PlayerAnimation>();
    }

    void Update()
    {
        // Update timers
        if (velocityLockTimer > 0f)
            velocityLockTimer -= Time.deltaTime;

        if (jumpBufferTimer > 0f)
            jumpBufferTimer -= Time.deltaTime;

        if (coyoteTimer > 0f)
            coyoteTimer -= Time.deltaTime;

        // Movement: don't overwrite X when a velocity lock is active
        if (velocityLockTimer > 0f)
        {
            // do nothing on X
        }
        else
        {
            // set horizontal velocity safely
            Vector2 target = new Vector2(horizontalMovement * speed, rb != null ? rb.linearVelocity.y : 0f);
            SetClampedVelocity(target, "Update movement");
        }

        // Ground check must run before using grounded state for jump buffering
        GroundedCheck();
        Gravity();

        // If we have a buffered jump and we can jump (either on ground or within coyote time), do it immediately
        if (jumpBufferTimer > 0f && (jumpRemaining > 0 || coyoteTimer > 0f))
        {
            ExecuteJump();
        }

        // optional debug
        // Debug.Log(rb.linearVelocity);
    }

    private void Gravity()
    {
        float vy = rb != null ? rb.linearVelocity.y : 0f;
        if (vy < -0.01f)
        {
            rb.gravityScale = baseGravity * fallSpeedMultiplier;
            float clampedY = Mathf.Max(vy, maxFallSpeed);
            SetClampedVelocity(new Vector2(rb.linearVelocity.x, clampedY), "Gravity");
        }
        else
        {
            rb.gravityScale = baseGravity;
        }
    }

    // Called by PlayerInput when move input changes
    public void Move(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            horizontalMovement = context.ReadValue<Vector2>().x;

            // Apply deadzone: treat small axis values as zero so "no input" is stable
            if (Mathf.Abs(horizontalMovement) < inputDeadzone)
                horizontalMovement = 0f;

            HorizontalInput = horizontalMovement;

            if (horizontalMovement > 0)
                facingDirection = 1;
            else if (horizontalMovement < 0)
                facingDirection = -1;
        }
        else if (context.canceled)
        {
            horizontalMovement = 0f;
            HorizontalInput = 0f;
        }
    }

    // Jump input - buffer performed, apply short-hop on release
    public void Jump(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            // buffer jump input
            jumpBufferTimer = jumpBufferTime;

            // start jump animation immediately on button press
            playerAnimation?.PlayJumpTrigger();
        }
        else if (context.canceled)
        {
            // short hop: if still rising, reduce upward velocity
            if (rb.linearVelocity.y > 0f)
                SetClampedVelocity(new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f), "Short hop");
        }
    }

    private void ExecuteJump()
    {
        // Only jump if we have jumps remaining or are in coyote window
        if (jumpRemaining <= 0 && coyoteTimer <= 0f)
            return;

        SetClampedVelocity(new Vector2(rb.linearVelocity.x, jumpForce), "ExecuteJump");

        // consume a jump if available, otherwise consume coyote time
        if (jumpRemaining > 0)
            jumpRemaining--;
        else
            coyoteTimer = 0f;

        jumpBufferTimer = 0f;
    }

    private void GroundedCheck()
    {
        bool grounded = Physics2D.OverlapBox(groundCheckPos.position, groundCheckSize, 0, groundLayer);

        // landed this frame -> refill jumps
        if (grounded && !wasGrounded)
        {
            jumpRemaining = Mathf.Max(1, maxJump);
        }

        // left ground this frame -> start coyote window
        if (!grounded && wasGrounded)
        {
            coyoteTimer = coyoteTime;
        }

        wasGrounded = grounded;
        IsGrounded = grounded;
    }

    // Called by other systems (attacks, dash, etc.) to apply a velocity and prevent movement from overwriting it
    public void ApplyVelocityLock(Vector2 velocity, float duration)
    {
        if (rb == null) return;

        SetClampedVelocity(velocity, "ApplyVelocityLock");
        velocityLockTimer = Mathf.Max(velocityLockTimer, duration);
    }

    // helper to safely assign rb.linearVelocity with clamping + logging
    private void SetClampedVelocity(Vector2 v, string source)
    {
        if (rb == null) return;

        // guard non-finite
        if (!float.IsFinite(v.x) || !float.IsFinite(v.y))
        {
            Debug.LogWarning($"[PlayerMovement] {source} attempted to set non-finite velocity ({v}). Zeroing.");
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // clamp magnitude
        if (v.magnitude > maxVelocityMagnitude)
        {
            Debug.LogWarning($"[PlayerMovement] {source} attempted to set a large velocity ({v.magnitude}). Clamping to {maxVelocityMagnitude}.");
            v = Vector2.ClampMagnitude(v, maxVelocityMagnitude);
        }

        rb.linearVelocity = v;
    }

    public void OnDrawGizmosSelected()
    {
        if (groundCheckPos == null) return;
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(groundCheckPos.position, groundCheckSize);
    }
}