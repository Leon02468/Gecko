using UnityEngine;

public class MobMovement : MonoBehaviour
{
    [SerializeField] private float speed = 2f;                   // units per second when patrolling and returning
    [SerializeField] private float patrolDistance = 3f;          // distance from start position to turn around
    [SerializeField] private float outOfBoundsTolerance = 0.1f;  // how far beyond boundary counts as "knocked out"
    [SerializeField] private float arrivalThreshold = 0.05f;     // threshold to consider we've reached the boundary target
    [SerializeField] private float velocityAcceleration = 8f;    // how fast the rb velocity is pulled back to patrol speed

    // Anti-jitter
    // Preferably
    // flipDeadzone: 0.03–0.2 (world units)
    // flipCooldown: 0.12–0.4 seconds
    [SerializeField] private float flipDeadzone = 0.05f;         // small extra margin to avoid flipping exactly at boundary
    [SerializeField] private float flipCooldown = 0.18f;         // minimum seconds between flips

    // --- Visualization ---
    [Header("Visualization")]
    [SerializeField] private bool showGizmos = true;             // toggle gizmo drawing
    [SerializeField] private Color gizmoColor = new Color(0f, 0.6f, 1f, 0.25f);
    [SerializeField] private Color gizmoWireColor = new Color(0f, 0.6f, 1f, 1f);
    [SerializeField] private float gizmoPadding = 0.08f;         // extra padding around sprite when drawing box
    [SerializeField] private float pillarHeight = 2f;            // vertical size of endpoint "pillar" visualization

    // Optional: sprite renderer reference (kept for sizing only)
    private SpriteRenderer spriteRenderer;

    private Vector3 startPosition;
    private int direction = 1; // 1 = right, -1 = left

    private enum State { Patrolling, Returning }
    private State state = State.Patrolling;
    private float returnTargetX;

    // Physics support
    private Rigidbody2D rb2d;
    private bool hasRb => rb2d != null;

    // runtime
    private float lastFlipTime = -10f;

    void Start()
    {
        startPosition = transform.position;
        rb2d = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    void OnValidate()
    {
        // keep a reference to SpriteRenderer in editor if possible
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    void Update()
    {
        // Non-physics movement runs in Update
        if (!hasRb)
            UpdateTransformMovement();
    }

    void FixedUpdate()
    {
        if (!hasRb)
            return;

        UpdateLogicOnly();

        // Branch based on Rigidbody2D body type
        if (rb2d.bodyType == RigidbodyType2D.Dynamic)
            FixedPhysicsMovementDynamic();
        else // Kinematic or Static (we'll treat Kinematic specially)
            FixedPhysicsMovementKinematic();
    }

    // Shared logic: state transitions & target decisions (run inside FixedUpdate for physics)
    private void UpdateLogicOnly()
    {
        Vector2 physPos = hasRb ? rb2d.position : (Vector2)transform.position;
        float offset = physPos.x - startPosition.x;
        float absOffset = Mathf.Abs(offset);

        switch (state)
        {
            case State.Patrolling:
                // If pushed beyond the tolerance, start returning
                if (absOffset > patrolDistance + outOfBoundsTolerance)
                {
                    returnTargetX = startPosition.x + Mathf.Sign(offset) * patrolDistance;
                    state = State.Returning;

                    // Immediately face the return direction so the sprite doesn't "moonwalk"
                    FaceReturnDirection(physPos.x);
                }
                else
                {
                    // Only flip when actually moving outward and after a small deadzone/cooldown to avoid jitter
                    bool movingOutward = true;
                    if (hasRb)
                        movingOutward = rb2d.linearVelocity.x * direction > 0.01f;
                    else
                        movingOutward = Mathf.Sign(offset) == direction;

                    if (absOffset >= patrolDistance + flipDeadzone && movingOutward && Time.time - lastFlipTime >= flipCooldown)
                    {
                        // flip facing, but do NOT hard-set velocity here — let FixedUpdate smoothly steer velocity
                        direction *= -1;
                        ApplyFacing();
                        lastFlipTime = Time.time;
                    }
                }
                break;

            case State.Returning:
                // If pushed again while returning, update target to nearest boundary
                if (absOffset > patrolDistance + outOfBoundsTolerance)
                {
                    returnTargetX = startPosition.x + Mathf.Sign(offset) * patrolDistance;

                    // also update facing immediately to the new return direction
                    FaceReturnDirection(physPos.x);
                }

                // If we arrived at the return target (within threshold), switch back to patrolling
                if (Mathf.Abs(physPos.x - returnTargetX) <= arrivalThreshold)
                {
                    // snap to exact boundary to avoid tiny jitter
                    if (hasRb)
                    {
                        rb2d.position = new Vector2(returnTargetX, rb2d.position.y);

                        // set patrol direction so the mob moves back toward the center from the boundary
                        float boundaryRelative = returnTargetX - startPosition.x;
                        direction = boundaryRelative < 0f ? 1 : -1;

                        // continue moving inward seamlessly at patrol speed
                        rb2d.linearVelocity = new Vector2(direction * speed, rb2d.linearVelocity.y);
                    }
                    else
                    {
                        Vector3 p = transform.position;
                        p.x = returnTargetX;
                        transform.position = p;

                        float boundaryRelative = returnTargetX - startPosition.x;
                        direction = boundaryRelative < 0f ? 1 : -1;
                    }

                    ApplyFacing();
                    state = State.Patrolling;
                    lastFlipTime = Time.time; // avoid immediately flipping again
                }
                break;
        }
    }

    // Transform-based movement (no Rigidbody2D)
    private void UpdateTransformMovement()
    {
        switch (state)
        {
            case State.Patrolling:
                transform.Translate(direction * speed * Time.deltaTime, 0f, 0f);
                break;
            case State.Returning:
                float newX = Mathf.MoveTowards(transform.position.x, returnTargetX, speed * Time.deltaTime);
                Vector3 pos = transform.position;
                pos.x = newX;
                transform.position = pos;

                // Face movement direction while returning
                int returnDir = returnTargetX > transform.position.x ? 1 : -1;
                if (returnDir != direction)
                {
                    direction = returnDir;
                    ApplyFacing();
                }
                break;
        }
    }

    // Dynamic body handling: use velocity blending so AddForce knockbacks are respected.
    // Returning blends current velocity smoothly toward the return direction and desired speed.
    private void FixedPhysicsMovementDynamic()
    {
        switch (state)
        {
            case State.Patrolling:
                {
                    float targetVx = direction * speed;
                    float newVx = Mathf.MoveTowards(rb2d.linearVelocity.x, targetVx, velocityAcceleration * Time.fixedDeltaTime);
                    rb2d.linearVelocity = new Vector2(newVx, rb2d.linearVelocity.y);
                }
                break;

            case State.Returning:
                {
                    float diff = returnTargetX - rb2d.position.x;
                    if (Mathf.Abs(diff) <= arrivalThreshold)
                    {
                        // arrival handled in UpdateLogicOnly (we still early-return here)
                        return;
                    }

                    int moveDir = diff > 0f ? 1 : -1;

                    // Preserve current inbound speed if it's already moving inward, otherwise ensure at least 'speed'
                    float currentSpeed = Mathf.Abs(rb2d.linearVelocity.x);
                    float desiredSpeed = Mathf.Max(currentSpeed, speed);

                    // If current velocity is away from target, we want to accelerate toward desired inbound speed.
                    float targetReturnVx = moveDir * desiredSpeed;

                    float blendedVx = Mathf.MoveTowards(rb2d.linearVelocity.x, targetReturnVx, velocityAcceleration * Time.fixedDeltaTime);
                    rb2d.linearVelocity = new Vector2(blendedVx, rb2d.linearVelocity.y);

                    // Ensure sprite faces return direction while returning (prevents moonwalk)
                    FaceReturnDirection(rb2d.position.x);
                }
                break;
        }
    }

    // Kinematic body handling: move via MovePosition so the body actually moves when kinematic
    private void FixedPhysicsMovementKinematic()
    {
        switch (state)
        {
            case State.Patrolling:
                Vector2 nextPos = rb2d.position + new Vector2(direction * speed * Time.fixedDeltaTime, 0f);
                rb2d.MovePosition(nextPos);
                break;

            case State.Returning:
                float newX = Mathf.MoveTowards(rb2d.position.x, returnTargetX, speed * Time.fixedDeltaTime);
                rb2d.MovePosition(new Vector2(newX, rb2d.position.y));
                break;
        }
    }

    // Face the return direction immediately (doesn't change patrol 'direction' value).
    private void FaceReturnDirection(float currentX)
    {
        int returnDir = (returnTargetX > currentX) ? 1 : -1;

        // Always flip via scale.x (no flipX)
        Vector3 ls = transform.localScale;
        ls.x = Mathf.Abs(ls.x) * returnDir;
        transform.localScale = ls;
    }

    private void ApplyFacing()
    {
        // Always flip via scale.x (no flipX)
        Vector3 ls = transform.localScale;
        ls.x = Mathf.Abs(ls.x) * direction;
        transform.localScale = ls;
    }

    // Draw boundary boxes in the Scene view (and Game view if Gizmos enabled).
    private void OnDrawGizmos()
    {
        if (!showGizmos)
            return;

        // choose origin: use startPosition at runtime, otherwise use current transform for editor preview
        Vector3 origin = Application.isPlaying ? startPosition : transform.position;

        // try to get a reasonable sprite size
        Vector3 size = Vector3.one;
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            // use sprite width and pillarHeight for vertical size
            size = new Vector3(spriteRenderer.bounds.size.x + gizmoPadding, pillarHeight, spriteRenderer.bounds.size.z);
        }
        else
        {
            // fall back to object local scale as approximation
            size = new Vector3(1f + gizmoPadding, pillarHeight, 0.1f);
        }

        // compute boundary centers
        Vector3 leftCenter = new Vector3(origin.x - patrolDistance, transform.position.y, transform.position.z);
        Vector3 rightCenter = new Vector3(origin.x + patrolDistance, transform.position.y, transform.position.z);

        // filled semi-transparent boxes
        Color fill = gizmoColor;
        Gizmos.color = new Color(fill.r, fill.g, fill.b, Mathf.Clamp01(fill.a));
        Gizmos.DrawCube(leftCenter, size);
        Gizmos.DrawCube(rightCenter, size);

        // draw wireboxes and connector line
        Color wire = gizmoWireColor;
        Gizmos.color = wire;
        Gizmos.DrawWireCube(leftCenter, size);
        Gizmos.DrawWireCube(rightCenter, size);
        Gizmos.DrawLine(leftCenter, rightCenter);

        // draw box around current sprite (for reference)
        Vector3 currentCenter = new Vector3(transform.position.x, transform.position.y, transform.position.z);
        Vector3 currentSize = spriteRenderer != null ? new Vector3(spriteRenderer.bounds.size.x + gizmoPadding, spriteRenderer.bounds.size.y + gizmoPadding, spriteRenderer.bounds.size.z) : new Vector3(1f + gizmoPadding, 1f + gizmoPadding, 0.1f);
        Gizmos.DrawWireCube(currentCenter, currentSize);
    }
}