using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class PlayerAnimation : MonoBehaviour
{
    public Animator animator; // assign or auto-find
    public PlayerMovement playerMovement; // optional, auto-find
    public Rigidbody2D rb; // optional, auto-find

    [Header("Parameter Names")]
    public string paramSpeed = "Speed";
    public string paramVelY = "VelY";
    public string paramIsGrounded = "IsGrounded";
    public string paramJumpTrigger = "Jump"; // trigger name for immediate jump transitions

    [Header("Safety / clamping")]
    [Tooltip("Clamp Speed parameter sent to Animator to avoid extremely large values.")]
    public float maxReportedSpeed = 25f;     // clamp Speed parameter to this
    [Tooltip("Minimum reported Speed to feed animator (below this is treated as zero).")]
    public float minReportedSpeed = 0.01f;

    SpriteRenderer spriteRenderer;

    void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // If animation is driven by physics (velocity from Rigidbody), prefer AnimatePhysics so transitions
        // are evaluated in sync with physics. This helps remove one-frame latency between physics change and animator.
        if (animator != null && rb != null)
            animator.updateMode = AnimatorUpdateMode.Fixed;
    }

    void Update()
    {
        if (animator == null) return;

        float velX = 0f;
        float velY = 0f;

        // Prefer explicit input (clean, zero when no input)
        if (playerMovement != null)
        {
            float input = playerMovement.HorizontalInput;
            velX = input * playerMovement.speed;
            // vertical velocity still comes from physics if available
            velY = rb != null ? rb.linearVelocity.y : 0f;
        }
        else if (rb != null)
        {
            velX = rb.linearVelocity.x;
            velY = rb.linearVelocity.y;
        }

        // guard non-finite
        if (!float.IsFinite(velX)) velX = 0f;
        if (!float.IsFinite(velY)) velY = 0f;

        // convert to animator Speed param
        float rawSpeed = Mathf.Abs(velX);
        // apply minimum threshold so tiny drift -> zero
        float speedParam = rawSpeed < minReportedSpeed ? 0f : rawSpeed;
        // clamp maximum
        speedParam = Mathf.Clamp(speedParam, 0f, maxReportedSpeed);

        animator.SetFloat(paramSpeed, speedParam);
        animator.SetFloat(paramVelY, velY);

        if (playerMovement != null)
            animator.SetBool(paramIsGrounded, playerMovement.IsGrounded);

        // optional: flip sprite based on facing
        if (playerMovement != null && spriteRenderer != null)
            spriteRenderer.flipX = playerMovement.facingDirection < 0;
    }

    // Call this from PlayerMovement immediately after performing the jump to force the animation transition.
    // Using a trigger (and Any State transitions) allows the Animator to interrupt Idle/Run immediately.
    public void PlayJumpTrigger()
    {
        if (animator == null) return;
        animator.SetTrigger(paramJumpTrigger);
        // Also proactively set IsGrounded = false to help parameter-based transitions (defensive)
        animator.SetBool(paramIsGrounded, false);
    }
}