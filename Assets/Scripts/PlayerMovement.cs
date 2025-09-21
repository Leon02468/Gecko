using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public Rigidbody2D rb;

    [Header("Movement")]
    public float speed = 5f;
    float horizontalMovement = 0f;

    [Header("Jumping")]
    public float jumpForce = 10f;
    public int maxJump = 1;
    int jumpRemaining;

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
            rb.linearVelocity = new Vector2(horizontalMovement * speed, rb.linearVelocity.y);
        }

        // Ground check must run before using grounded state for jump buffering
        GroundedCheck();
        Gravity();

        // If we have a buffered jump and we can jump (either on ground or within coyote time), do it immediately
        if (jumpBufferTimer > 0f && (jumpRemaining > 0 || coyoteTimer > 0f))
        {
            ExecuteJump();
        }

        Debug.Log(rb.linearVelocity);
    }

    private void Gravity()
    {
        if (rb.linearVelocity.y < -0.01f)
        {
            rb.gravityScale = baseGravity * fallSpeedMultiplier;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Max(rb.linearVelocity.y, maxFallSpeed));
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
            if (horizontalMovement > 0)
                facingDirection = 1;
            else if (horizontalMovement < 0)
                facingDirection = -1;
        }
        else if (context.canceled)
        {
            horizontalMovement = 0f;
        }
    }

    // Jump input - buffer performed, apply short-hop on release
    public void Jump(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            // buffer jump input
            jumpBufferTimer = jumpBufferTime;
        }
        else if (context.canceled)
        {
            // short hop: if still rising, reduce upward velocity
            if (rb.linearVelocity.y > 0f)
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);
        }
    }

    private void ExecuteJump()
    {
        // Only jump if we have jumps remaining or are in coyote window
        if (jumpRemaining <= 0 && coyoteTimer <= 0f)
            return;

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);

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
        rb.linearVelocity = velocity;
        velocityLockTimer = Mathf.Max(velocityLockTimer, duration);
    }

    public void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(groundCheckPos.position, groundCheckSize);
    }
}