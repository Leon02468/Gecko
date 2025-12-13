using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour
{
    [Header("Side Attack (X)")]
    public Vector2 sideAttackBoxSize = new Vector2(1f, 1f);
    public float sideAttackRange = 1f;
    public float sideAttackCooldown = 0.5f;
    public int sideAttackDamage = 1;

    [Header("Top Attack")]
    public Vector2 topAttackBoxSize = new Vector2(1f, 1f);
    public float topAttackRange = 1f;
    public float topAttackCooldown = 0.5f;
    public int topAttackDamage = 1;

    [Header("Down Attack")]
    public Vector2 downAttackBoxSize = new Vector2(1f, 1f);
    public float downAttackRange = 1f;
    public float downAttackCooldown = 0.6f;
    public int downAttackDamage = 1;

    [Header("Attack Layer")]
    public LayerMask attackLayer;

    [Header("Tag based detection")]
    public bool useTag = true;
    public string enemyTag = "Enemy";

    [Header("Knockback")]
    public float knockbackForce = 6f;
    public float knockbackUpMultiplier = 0.4f; // upward portion for side knockback

    [Header("Down Attack Knockback")]
    [Tooltip("Multiplier for down attack knockback force (applied to knockbackForce)")]
    public float downAttackKnockbackMultiplier = 1.5f;
    [Tooltip("Horizontal component of down attack knockback (0-1, where 1 = full knockbackForce)")]
    public float downAttackKnockbackHorizontal = 0.8f;
    [Tooltip("Vertical component of down attack knockback (negative = downward, 0-1 range)")]
    public float downAttackKnockbackVertical = -0.6f;

    [Header("Down Attack Boost")]
    public float downAttackBoost = 10f; // overall magnitude
    public float downAttackHorizontalMultiplier = 1.0f; // multiplies X (1 = equal X/Y)
    public float downAttackLockDuration = 0.15f; // how long movement won't overwrite the boost

    [Header("Down Attack Moving Hitbox")]
    public float downAttackHitboxDuration = 0.18f;   // how long the moving hitbox follows player
    public float downAttackHitboxInterval = 0.02f;   // sampling interval (seconds)

    [Header("Down Attack Bounce (player)")]
    public float downAttackBounce = 8f; // vertical bounce applied to player when down-attack hits
    public float downAttackBounceLock = 0.12f; // how long player movement is locked while bouncing

    [Header("Down Attack Charging")]
    [Tooltip("Duration of the charging pause before executing the diagonal down attack (0 = no pause)")]
    public float downAttackChargeDuration = 0.15f; // brief pause/freeze before attacking
    [Tooltip("Freeze completely in place (true), or allow some horizontal drift during charge (false)")]
    public bool freezeVelocityDuringCharge = true;
    [Tooltip("When freeze is disabled, multiply horizontal speed by this amount during charge (0 = no drift, 1 = normal drift). Vertical is always frozen.")]
    public float chargeFallSpeedMultiplier = 0.0f; // controls horizontal drift during charge
    [Tooltip("Allow player to control horizontal movement during charge (overrides freeze setting for horizontal axis)")]
    public bool allowHorizontalControlDuringCharge = false;
    [Tooltip("Speed multiplier for horizontal movement during charge (0 = no control, 1 = full speed, 0.5 = half speed)")]
    public float chargeHorizontalControlMultiplier = 0.5f;

    [Header("Down Attack Invincibility")]
    [Tooltip("Grant player invincibility frames during down attack execution")]
    public bool grantInvincibilityDuringDownAttack = true;
    [Tooltip("Duration of invincibility during down attack (usually total of charge + attack + recovery)")]
    public float downAttackInvincibilityDuration = 0.5f;

    [Header("Down Attack Options")]
    [Tooltip("When enabled the down attack will only work while the player is airborne (grounded = false).")]
    public bool downAttackRequireAir = true;

    [Header("Jump/Thrust Options")]
    [Tooltip("Horizontal thrust applied for the ground second attack (Attack2) ÅEapplied as velocity lock")]
    public float thrustForce = 12f;
    [Tooltip("Duration to lock player velocity when thrusting")]
    public float thrustLockDuration = 0.12f;
    [Tooltip("How long the thrust moving hitbox stays active (in seconds)")]
    public float thrustHitboxDuration = 0.3f;
    [Tooltip("Sampling interval for the thrust moving hitbox (in seconds)")]
    public float thrustHitboxInterval = 0.02f;

    [Header("Thrust Hitbox")]
    [Tooltip("When true the thrust moving hitbox will use the side attack box/ range. If false, use custom size/range below.")]
    public bool thrustUseSideBox = true;
    [Tooltip("Custom hitbox size to use for thrust if not using side box")]
    public Vector2 thrustBoxSize = new Vector2(1f, 1f);
    [Tooltip("Custom hitbox range (distance from player) to use for thrust if not using side box")]
    public float thrustBoxRange = 1f;

    [Header("Animation Triggers")]
    [Tooltip("Trigger used for the airborne side attack. Default: JumpAttack (create this in Animator)")]
    public string airAttackTrigger = "JumpAttack";
    [Tooltip("Trigger used for the diagonal down attack. Default: JumpKick (create this in Animator)")]
    public string downAttackTrigger = "JumpKick";

    [Header("Gizmo Flash")]
    public float flashDuration = 0.3f; // How long the flash lasts (seconds)
    private float flashTimer = 0f;

    [Header("Animation")]
    [Tooltip("Animator used for attack animations. Triggers used: Attack1, Attack2, JumpAttack")]
    public Animator animator;

    // separate last-used timestamps per attack
    private float lastSideAttackTime = -Mathf.Infinity;
    private float lastTopAttackTime = -Mathf.Infinity;
    private float lastDownAttackTime = -Mathf.Infinity;

    private PlayerMovement playerMovement;
    private PlayerHealth playerHealth;

    // For gizmo direction/type
    private enum AttackType { Side, Top, Down }
    private AttackType lastAttackType = AttackType.Side;

    // coroutine handle so subsequent down-attacks can be ignored/replace previous
    private Coroutine downHitboxCoroutine;

    // Combo handling for side attacks (2-stage)
    [Header("Combo")]
    [Tooltip("Time window after the first side attack during which a second press will chain into Attack2 (thrust).")]
    public float sideComboWindow = 0.35f;
    private bool comboWaiting = false; // waiting for second press
    private Coroutine comboCoroutine = null;

    // prevent multiple air attacks in short succession
    private bool airAttackInProgress = false;

    // prevent down attack from triggering repeatedly (for composite bindings)
    private bool downAttackInProgress = false;

    void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        playerHealth = GetComponent<PlayerHealth>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    // Main attack (X) - checks for down input to trigger jump kick
    public void OnAttack(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        bool isAir = playerMovement != null && !playerMovement.IsGrounded;

        // Check if player is holding down while pressing attack AND is airborne
        if (isAir && playerMovement != null && playerMovement.IsHeldDown)
        {
            // Perform jump kick instead of normal attack (only if not already in progress)
            if (!downAttackInProgress)
            {
                HandleDownAttackInput();
            }
            return;
        }

        // Normal side attack (on ground or in air)
        PerformNormalSideAttackWithCooldown();
    }

    private void PerformNormalSideAttackWithCooldown()
    {
        // If not waiting for combo, respect cooldown
        if (!comboWaiting && Time.time < lastSideAttackTime + sideAttackCooldown)
            return;
        bool isAir = playerMovement != null && !playerMovement.IsGrounded;

        // Play attack sound
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayPlayerAttacking();

        // If airborne, do single air attack and block further air attacks until cooldown
        if (isAir)
        {
            if (airAttackInProgress) return;
            airAttackInProgress = true;
            // trigger air attack animation (separate from ground Attack2)
            animator?.SetTrigger(airAttackTrigger);
            lastAttackType = AttackType.Side;
            flashTimer = flashDuration;

            PerformSideAttack();
            lastSideAttackTime = Time.time;
            StartCoroutine(ResetAirAttackAfterCooldown(sideAttackCooldown));
            return;
        }

        // If we're waiting for the second press (combo)
        if (comboWaiting)
        {
            // chain Attack2: play Attack2 animation, apply thrust and moving hitbox
            animator?.SetTrigger("Attack2");
            lastAttackType = AttackType.Side;
            flashTimer = flashDuration;

            // Determine facing
            int facing = 1;
            if (playerMovement != null)
                facing = playerMovement.facingDirection != 0 ? playerMovement.facingDirection : 1;

            // Apply horizontal thrust (no upward jump)
            Vector2 thrustVel = new Vector2(thrustForce * facing, 0f);
            if (playerMovement != null)
                playerMovement.ApplyVelocityLock(thrustVel, thrustLockDuration);

            // Start moving hitbox that follows player in the horizontal side direction with horizontal knockback
            Vector2 sideDir = (facing == 1) ? Vector2.right : Vector2.left;

            if (downHitboxCoroutine != null)
                StopCoroutine(downHitboxCoroutine);
            downHitboxCoroutine = StartCoroutine(RunMovingHitbox(sideDir, false, true, thrustHitboxDuration, thrustHitboxInterval, true)); // use side box with thrust timing and horizontal knockback

            // consume combo and start cooldown
            if (comboCoroutine != null) { StopCoroutine(comboCoroutine); comboCoroutine = null; }
            comboWaiting = false;
            lastSideAttackTime = Time.time;
            return;
        }

        // Otherwise start a fresh Attack1
        lastSideAttackTime = Time.time;
        lastAttackType = AttackType.Side;
        flashTimer = flashDuration;

        animator?.SetTrigger("Attack1");

        // open combo window on ground so a subsequent press within the window will chain
        if (comboCoroutine != null)
            StopCoroutine(comboCoroutine);
        comboWaiting = true;
        comboCoroutine = StartCoroutine(ComboWindow());

        PerformSideAttack();
    }

    private IEnumerator ResetAirAttackAfterCooldown(float cooldown)
    {
        yield return new WaitForSeconds(cooldown);
        airAttackInProgress = false;
    }

    private IEnumerator ComboWindow()
    {
        float t = 0f;
        while (t < sideComboWindow)
        {
            t += Time.deltaTime;
            yield return null;
        }

        // combo window expired
        comboWaiting = false;
        comboCoroutine = null;
    }

    // Explicit top attack binding (Up + X or separate action)
    public void OnTopAttack(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        // Do not allow top attack while airborne
        if (playerMovement != null && !playerMovement.IsGrounded)
            return;

        if (Time.time < lastTopAttackTime + topAttackCooldown)
            return;

        lastTopAttackTime = Time.time;
        lastAttackType = AttackType.Top;
        flashTimer = flashDuration;

        // Animator: top attack (keep as Attack2 or change in Animator as needed)
        animator?.SetTrigger("Attack2");

        PerformTopAttack();
    }

    // Shared down attack logic - now uses JumpAttack animation with diagonal boost
    private void HandleDownAttackInput()
    {
        // If down-attack is restricted to air and player is grounded -> do nothing
        if (downAttackRequireAir && playerMovement != null && playerMovement.IsGrounded)
            return;

        // Gate hit (and boost) by down attack cooldown
        if (Time.time < lastDownAttackTime + downAttackCooldown)
            return;

        // Set the in-progress flag and start the cooldown coroutine
        downAttackInProgress = true;
        StartCoroutine(ResetDownAttackAfterCooldown(downAttackCooldown));

        lastDownAttackTime = Time.time;
        lastAttackType = AttackType.Down;
        flashTimer = flashDuration;

        // Use JumpAttack animation for diagonal down attack (no separate JumpKick parameter)
        animator?.SetTrigger(airAttackTrigger); // Uses "JumpAttack" trigger

        // Start charging coroutine that freezes briefly then executes the attack
        StartCoroutine(DownAttackChargeAndExecute());
    }

    private IEnumerator DownAttackChargeAndExecute()
    {
        // Determine facing before charge
        int facing = 1;
        if (playerMovement != null)
            facing = playerMovement.facingDirection != 0 ? playerMovement.facingDirection : 1;

        // Charging pause: freeze player at current Y position in air briefly
        if (downAttackChargeDuration > 0f && playerMovement != null && playerMovement.rb != null)
        {
            float elapsed = 0f;
            float startY = transform.position.y;
            
            if (freezeVelocityDuringCharge && !allowHorizontalControlDuringCharge)
            {
                // Complete freeze: maintain exact position (no horizontal movement allowed)
                Vector2 startPos = transform.position;
                while (elapsed < downAttackChargeDuration)
                {
                    // Force position lock by setting velocity to zero every frame
                    playerMovement.rb.linearVelocity = Vector2.zero;
                    
                    elapsed += Time.deltaTime;
                    yield return null;
                }
            }
            else if (allowHorizontalControlDuringCharge)
            {
                // Allow horizontal player control during charge, freeze Y position
                while (elapsed < downAttackChargeDuration)
                {
                    // Read horizontal input from PlayerMovement
                    float horizontalInput = playerMovement.HorizontalInput;
                    
                    // Apply horizontal movement based on input and control multiplier
                    float horizontalVelocity = horizontalInput * playerMovement.speed * chargeHorizontalControlMultiplier;
                    
                    // Set velocity: horizontal control + zero vertical (freeze Y position)
                    playerMovement.rb.linearVelocity = new Vector2(horizontalVelocity, 0f);
                    
                    elapsed += Time.deltaTime;
                    yield return null;
                }
            }
            else
            {
                // Freeze Y position only: preserve existing horizontal momentum, zero out vertical velocity
                float startX = transform.position.x;
                while (elapsed < downAttackChargeDuration)
                {
                    Vector2 currentVel = playerMovement.rb.linearVelocity;
                    // Maintain Y position by zeroing Y velocity, allow horizontal drift
                    playerMovement.rb.linearVelocity = new Vector2(currentVel.x * chargeFallSpeedMultiplier, 0f);
                    
                    elapsed += Time.deltaTime;
                    yield return null;
                }
            }
        }

        // Execute the diagonal boost after charging
        float boostX = downAttackBoost * downAttackHorizontalMultiplier * (facing == 1 ? 1f : -1f);
        float boostY = -downAttackBoost;
        Vector2 boostVelocity = new Vector2(boostX, boostY);

        if (playerMovement != null)
            playerMovement.ApplyVelocityLock(boostVelocity, downAttackLockDuration);

        // start moving hitbox coroutine (cancels previous if active)
        Vector2 diag = (facing == 1)
            ? new Vector2(downAttackHorizontalMultiplier, -1f).normalized
            : new Vector2(-downAttackHorizontalMultiplier, -1f).normalized;

        if (downHitboxCoroutine != null)
            StopCoroutine(downHitboxCoroutine);
        downHitboxCoroutine = StartCoroutine(RunMovingHitbox(diag, true)); // bounce player on down attack

        // Grant invincibility frames during down attack
        if (grantInvincibilityDuringDownAttack && playerHealth != null)
        {
            playerHealth.StartInvincibility(downAttackInvincibilityDuration);
        }
    }

    private IEnumerator ResetDownAttackAfterCooldown(float cooldown)
    {
        yield return new WaitForSeconds(cooldown);
        downAttackInProgress = false;
    }

    void Update()
    {
        if (flashTimer > 0f)
            flashTimer -= Time.deltaTime;
    }

    private bool IsValidTarget(Collider2D col)
    {
        if (col == null) return false;

        // never target self or any collider attached to this player
        if (IsSelf(col)) return false;

        if (useTag)
            return col.CompareTag(enemyTag);
        // fallback: use layer mask
        return (attackLayer.value & (1 << col.gameObject.layer)) != 0;
    }

    // helper to detect player's own colliders (or children)
    private bool IsSelf(Collider2D col)
    {
        if (col == null) return false;
        if (col.gameObject == gameObject) return true;
        if (col.transform.IsChildOf(transform)) return true;
        // also guard against hitting the player's Rigidbody2D directly
        if (playerMovement != null && playerMovement.rb != null && col.attachedRigidbody == playerMovement.rb) return true;
        return false;
    }

    // common hit handling: call damage on the target (health components handle knockback)
    private void HandleHit(Collider2D col, Vector2 knockback, int damage = 1, bool forceSetVelocity = false)
    {
        if (col == null) return;

        // Adjust knockback if caller requested forcing vertical-only velocity
        // (we encode that by zeroing horizontal component when forceSetVelocity == true)
        Vector2 kb = forceSetVelocity ? new Vector2(0f, knockback.y) : knockback;

        // **COUNTER-ATTACK**: Check if target is a mob performing charge attack
        var mobAI = col.GetComponentInParent<MobAI>();
        if (mobAI != null && mobAI.IsChargingAndVulnerable)
        {
            // Player counter-attacked a charging mob! Interrupt the charge
            Debug.Log($"<color=orange>COUNTER-ATTACK! Player hit {col.name} during charge attack!</color>");
            mobAI.InterruptChargeAttack(kb, damage);
            // Don't apply normal damage since InterruptChargeAttack handles it
            return;
        }

        // Play enemy get hit sound if the target is an enemy 
        if (AudioManager.Instance != null && col.CompareTag("Enemy"))
        {
            Debug.Log("Enemy hit sound should play now!");
            AudioManager.Instance.PlayEnemyGetHit();
        }
           

        // Normal damage handling
        var dmg = col.GetComponentInParent<IDamageable>();
        if (dmg != null)
        {
            dmg.TakeDamage(damage, kb);
        }
        else
        {
            // fallback for legacy scripts: try to send damage amount, then knockback separately
            col.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
            col.SendMessage("ApplyKnockback", kb, SendMessageOptions.DontRequireReceiver);
        }
    }

    private IEnumerator RunMovingHitbox(Vector2 diag, bool bouncePlayer, bool useSideBox = false, float? customDuration = null, float? customInterval = null, bool useHorizontalKnockback = false)
    {
        float timer = 0f;
        var hitSet = new HashSet<Collider2D>();
        bool playerBounced = false;

        // choose box size and range based on caller preference (thrust uses side box)
        Vector2 boxSize = useSideBox ? sideAttackBoxSize : downAttackBoxSize;
        float range = useSideBox ? sideAttackRange : downAttackRange;
        
        // use custom duration/interval if provided, otherwise use down attack defaults
        float duration = customDuration ?? downAttackHitboxDuration;
        float interval = customInterval ?? downAttackHitboxInterval;
        
        // Determine which damage to use based on attack type
        int damageAmount = useSideBox ? sideAttackDamage : downAttackDamage;

        while (timer < duration)
        {
            Vector2 origin = (Vector2)transform.position + diag * range;

            // sample with layer mask
            Collider2D[] hits = Physics2D.OverlapBoxAll(origin, boxSize, 0f, attackLayer);

            foreach (var hit in hits)
            {
                if (hit == null) continue;
                if (hitSet.Contains(hit)) continue;
                if (!IsValidTarget(hit)) continue;

                // apply enemy knockback based on attack type
                int facing = playerMovement != null ? playerMovement.facingDirection : 1;
                Vector2 kb;
                
                if (useHorizontalKnockback)
                {
                    // Horizontal knockback for thrust attack (side direction with small upward component)
                    kb = new Vector2(facing * knockbackForce, knockbackForce * knockbackUpMultiplier);
                    Debug.Log($"Thrust Hit: {hit.name} with knockback {kb}");
                }
                else
                {
                    // Diagonal downward knockback for down attack
                    float baseKnockback = knockbackForce * downAttackKnockbackMultiplier;
                    kb = new Vector2(
                        facing * baseKnockback * downAttackKnockbackHorizontal,
                        baseKnockback * downAttackKnockbackVertical
                    );
                    Debug.Log($"Down Attack Hit: {hit.name} with diagonal knockback {kb}, damage: {damageAmount}");
                }
                
                HandleHit(hit, kb, damageAmount, false);
                hitSet.Add(hit);

                // bounce player if requested (only once)
                if (bouncePlayer && !playerBounced && playerMovement != null && playerMovement.rb != null)
                {
                    playerMovement.ApplyVelocityLock(new Vector2(playerMovement.rb.linearVelocity.x, downAttackBounce), downAttackBounceLock);
                    playerBounced = true;
                }
            }

            timer += interval;
            // draw for debugging briefly
            DebugDrawBox((Vector2)transform.position + diag * range, boxSize, Color.cyan, interval);

            yield return new WaitForSeconds(interval);
        }

        downHitboxCoroutine = null;
    }

    private void PerformSideAttack()
    {
        int facing = 1;
        if (playerMovement != null)
            facing = playerMovement.facingDirection != 0 ? playerMovement.facingDirection : 1;

        Vector2 dir = (facing == 1) ? Vector2.right : Vector2.left;
        Vector2 origin = (Vector2)transform.position + dir * sideAttackRange;

        Collider2D[] hits = Physics2D.OverlapBoxAll(origin, sideAttackBoxSize, 0f, attackLayer);
        foreach (var hit in hits)
        {
            if (!IsValidTarget(hit)) continue;

            // knockback horizontally away from player with small upward component
            Vector2 away = (hit.transform.position.x >= transform.position.x) ? Vector2.right : Vector2.left;
            Vector2 kb = away * knockbackForce + Vector2.up * (knockbackForce * knockbackUpMultiplier);
            HandleHit(hit, kb, sideAttackDamage, false);
            Debug.Log("Side Hit: " + hit.name);
            break;//only hit the first valid enemy
        }
    }

    private void PerformTopAttack()
    {
        Vector2 origin = (Vector2)transform.position + Vector2.up * topAttackRange;

        Collider2D[] hits = Physics2D.OverlapBoxAll(origin, topAttackBoxSize, 0f, attackLayer);
        foreach (var hit in hits)
        {
            // skip self explicitly before anything else
            if (IsSelf(hit)) continue;
            if (!IsValidTarget(hit)) continue;

            // knockback mostly upward (force vertical directly to avoid lateral motion)
            Vector2 kb = Vector2.up * knockbackForce;
            HandleHit(hit, kb, topAttackDamage, true); // forceSetVelocity = true
            Debug.Log("Top Hit: " + hit.name);
            break; //only hit the first valid enemy
        }
    }

    // helper debug drawer (draws wire rectangle centered at pos)
    private void DebugDrawBox(Vector2 center, Vector2 size, Color color, float duration = 0.1f)
    {
        Vector3 half = new Vector3(size.x, size.y, 0f) * 0.5f;
        Vector3 p1 = center + new Vector2(-half.x, -half.y);
        Vector3 p2 = center + new Vector2(half.x, -half.y);
        Vector3 p3 = center + new Vector2(half.x, half.y);
        Vector3 p4 = center + new Vector2(-half.x, half.y);
        Debug.DrawLine(p1, p2, color, duration);
        Debug.DrawLine(p2, p3, color, duration);
        Debug.DrawLine(p3, p4, color, duration);
        Debug.DrawLine(p4, p1, color, duration);
    }

    void OnDrawGizmos()
    {
        if (flashTimer <= 0f) return;

        Gizmos.color = Color.yellow;

        int facing = 1;
        if (playerMovement != null)
            facing = playerMovement.facingDirection != 0 ? playerMovement.facingDirection : 1;

        Vector2 origin = (Vector2)transform.position;
        Vector2 dir;
        Vector2 size;

        switch (lastAttackType)
        {
            case AttackType.Side:
                dir = (facing == 1) ? Vector2.right : Vector2.left;
                origin += dir * sideAttackRange;
                size = sideAttackBoxSize;
                break;
            case AttackType.Top:
                dir = Vector2.up;
                origin += dir * topAttackRange;
                size = topAttackBoxSize;
                break;
            default: // Down
                dir = (facing == 1)
                    ? new Vector2(downAttackHorizontalMultiplier, -1f).normalized
                    : new Vector2(-downAttackHorizontalMultiplier, -1f).normalized;
                origin += dir * downAttackRange;
                size = downAttackBoxSize;
                break;
        }

        Gizmos.DrawWireCube(origin, size);
    }
}