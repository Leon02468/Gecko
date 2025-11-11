using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class MobAnimation : MonoBehaviour
{
    [Header("References")]
    public Animator animator;                 // assign in inspector or auto-find
    public Rigidbody2D rb;                    // optional, auto-find
    public MonoBehaviour movementComponent;   // optional: any movement script (used only to detect movement if it exposes nothing)

    [Header("Animator parameter names")]
    public string paramSpeed = "Speed";
    public string paramVelY = "VelY";
    public string paramIsGrounded = "IsGrounded"; // optional if mob uses grounded logic
    public string triggerAttack = "Attack";
    public string triggerHurt = "Hurt";
    public string triggerDeath = "Death";

    [Header("Settings")]
    [Tooltip("Minimum horizontal speed to count as moving (so tiny drift won't trigger walk)")]
    public float minMoveSpeed = 0.05f;
    [Tooltip("If true flips sprite via SpriteRenderer.flipX; otherwise flips via localScale.x")]
    public bool useSpriteFlip = false;

    SpriteRenderer spriteRenderer;

    void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        // If we have an animator + rigidbody, evaluate in FixedUpdate to better sync with physics-driven movement
        if (animator != null && rb != null)
        {
            animator.updateMode = AnimatorUpdateMode.Fixed;
            animator.animatePhysics = true;
        }
    }

    void Update()
    {
        if (animator == null) return;

        float velX = 0f;
        float velY = 0f;

        if (rb != null)
        {
            var v = rb.linearVelocity;
            if (float.IsFinite(v.x)) velX = v.x;
            if (float.IsFinite(v.y)) velY = v.y;
        }

        // Drive animator parameters
        animator.SetFloat(paramSpeed, Mathf.Abs(velX));
        animator.SetFloat(paramVelY, velY);

        // flip sprite to face movement direction using localScale or flipX
        float facing = Mathf.Sign(transform.localScale.x);
        if (rb != null && Mathf.Abs(rb.linearVelocity.x) > minMoveSpeed)
            facing = Mathf.Sign(rb.linearVelocity.x);

        ApplyFacingInternal((int)Mathf.Sign(facing));
    }

    // Public helper: set facing immediately (1 = right, -1 = left)
    public void SetFacing(int dir)
    {
        if (dir == 0) return;
        ApplyFacingInternal(dir);
    }

    // Internal facing application
    private void ApplyFacingInternal(int dir)
    {
        if (dir == 0) return;

        if (spriteRenderer != null && useSpriteFlip)
        {
            spriteRenderer.flipX = dir < 0;
        }
        else
        {
            Vector3 s = transform.localScale;
            s.x = Mathf.Abs(s.x) * (dir < 0 ? -1f : 1f);
            transform.localScale = s;
        }
    }

    // Immediate transitions (use triggers so transitions don't require exit time)
    // Overload that accepts facing so caller can force direction before animation plays.
    public void PlayAttack(int facing = 0)
    {
        if (facing != 0) SetFacing(facing);
        if (animator == null) return;
        animator.ResetTrigger(triggerHurt);
        animator.ResetTrigger(triggerDeath);
        animator.SetTrigger(triggerAttack);
    }

    public void PlayHurt()
    {
        if (animator == null) return;
        animator.ResetTrigger(triggerAttack);
        animator.ResetTrigger(triggerDeath);
        animator.SetTrigger(triggerHurt);
    }

    public void PlayDeath()
    {
        if (animator == null) return;
        animator.ResetTrigger(triggerAttack);
        animator.ResetTrigger(triggerHurt);
        animator.SetTrigger(triggerDeath);
    }
}