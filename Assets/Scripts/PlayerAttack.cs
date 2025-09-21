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

    [Header("Top Attack")]
    public Vector2 topAttackBoxSize = new Vector2(1f, 1f);
    public float topAttackRange = 1f;
    public float topAttackCooldown = 0.5f;

    [Header("Down Attack")]
    public Vector2 downAttackBoxSize = new Vector2(1f, 1f);
    public float downAttackRange = 1f;
    public float downAttackCooldown = 0.6f;

    [Header("Attack Layer")]
    public LayerMask attackLayer;

    [Header("Tag based detection")]
    public bool useTag = true;
    public string enemyTag = "Enemy";

    [Header("Knockback")]
    public float knockbackForce = 6f;
    public float knockbackUpMultiplier = 0.4f; // upward portion for side knockback

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

    [Header("Down Attack Options")]
    [Tooltip("When enabled the down attack will only work while the player is airborne (grounded = false).")]
    public bool downAttackRequireAir = true;

    [Header("Gizmo Flash")]
    public float flashDuration = 0.15f; // How long the flash lasts (seconds)
    private float flashTimer = 0f;

    // separate last-used timestamps per attack
    private float lastSideAttackTime = -Mathf.Infinity;
    private float lastTopAttackTime = -Mathf.Infinity;
    private float lastDownAttackTime = -Mathf.Infinity;

    private PlayerMovement playerMovement;

    // For gizmo direction/type
    private enum AttackType { Side, Top, Down }
    private AttackType lastAttackType = AttackType.Side;

    // coroutine handle so subsequent down-attacks can be ignored/replace previous
    private Coroutine downHitboxCoroutine;

    void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
    }

    // Main attack (X). If player is holding down (keyboard/gamepad) this will attempt a down attack even in air.
    public void OnAttack(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        // If the player is holding "down" while pressing attack, prefer down-attack input.
        if (IsDownHeld())
        {
            // delegate to shared down-attack logic (this respects the down cooldown and air requirement)
            HandleDownAttackInput();
            return;
        }

        // Normal side attack
        if (Time.time < lastSideAttackTime + sideAttackCooldown)
            return;

        lastSideAttackTime = Time.time;
        lastAttackType = AttackType.Side;
        flashTimer = flashDuration;
        PerformSideAttack();
    }

    // Explicit top attack binding (Up + X or separate action)
    public void OnTopAttack(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        if (Time.time < lastTopAttackTime + topAttackCooldown)
            return;

        lastTopAttackTime = Time.time;
        lastAttackType = AttackType.Top;
        flashTimer = flashDuration;
        PerformTopAttack();
    }

    // If you have a dedicated Down+Attack action, it will call this.
    public void OnDownAttack(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        HandleDownAttackInput();
    }

    // Shared down attack logic (called from OnAttack when down held, or OnDownAttack)
    private void HandleDownAttackInput()
    {
        // If down-attack is restricted to air and player is grounded -> do nothing
        if (downAttackRequireAir && playerMovement != null && playerMovement.IsGrounded)
            return;

        // Gate hit (and boost) by down attack cooldown
        if (Time.time < lastDownAttackTime + downAttackCooldown)
            return;

        lastDownAttackTime = Time.time;
        lastAttackType = AttackType.Down;
        flashTimer = flashDuration;

        // Determine facing
        int facing = 1;
        if (playerMovement != null)
            facing = playerMovement.facingDirection != 0 ? playerMovement.facingDirection : 1;

        // Apply immediate diagonal boost (explicit X/Y so it's clearly diagonal)
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
        downHitboxCoroutine = StartCoroutine(RunMovingDownHitbox(diag));
    }

    void Update()
    {
        if (flashTimer > 0f)
            flashTimer -= Time.deltaTime;
    }

    private bool IsDownHeld()
    {
        // Check keyboard
        if (Keyboard.current != null)
        {
            if (Keyboard.current.downArrowKey.isPressed || Keyboard.current.sKey.isPressed)
                return true;
        }

        // Check gamepad dpad / left stick
        if (Gamepad.current != null)
        {
            if (Gamepad.current.dpad.down.isPressed)
                return true;

            // stick threshold
            if (Gamepad.current.leftStick.ReadValue().y < -0.5f)
                return true;
        }

        return false;
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

    // common hit handling: call damage and apply knockback
    // Modified: allow forcing vertical velocity assignment for top attacks to avoid side motion
    private void HandleHit(Collider2D col, Vector2 knockback, bool forceSetVelocity = false)
    {
        if (col == null) return;

        // Notify enemy of damage (optional)
        col.SendMessage("TakeDamage", SendMessageOptions.DontRequireReceiver);

        // Try to get the Rigidbody2D (attached or on the collider)
        Rigidbody2D enemyRb = col.attachedRigidbody != null ? col.attachedRigidbody : col.GetComponent<Rigidbody2D>();

        if (enemyRb != null)
        {
            // If caller requested direct velocity set (useful for vertical-only knockback),
            // replace horizontal velocity with zero and set vertical velocity.
            if (forceSetVelocity)
            {
                // Ensure we set velocity using the correct field depending on bodyType
                switch (enemyRb.bodyType)
                {
                    case RigidbodyType2D.Dynamic:
                    case RigidbodyType2D.Kinematic:
                        // zero horizontal, set vertical directly for a clean upward knock
                        enemyRb.linearVelocity = new Vector2(0f, knockback.y);
                        break;

                    case RigidbodyType2D.Static:
                        // Static can't move, small transform nudge
                        col.transform.position += (Vector3)(new Vector2(0f, knockback.y) * 0.02f);
                        break;
                }
                return;
            }

            switch (enemyRb.bodyType)
            {
                case RigidbodyType2D.Dynamic:
                    // Preferred: impulse respects mass
                    enemyRb.AddForce(knockback, ForceMode2D.Impulse);
                    break;

                case RigidbodyType2D.Kinematic:
                    // Kinematic bodies don't respond to AddForce — set velocity for immediate effect
                    enemyRb.linearVelocity = knockback;
                    break;

                case RigidbodyType2D.Static:
                    // Static bodies won't move; try slight transform shift (fallback)
                    col.transform.position += (Vector3)(knockback * 0.02f);
                    break;
            }
        }
        else
        {
            // No Rigidbody2D found — try calling a script method (e.g. enemy controller)
            col.SendMessage("ApplyKnockback", knockback, SendMessageOptions.DontRequireReceiver);
        }
    }

    private IEnumerator RunMovingDownHitbox(Vector2 diag)
    {
        float timer = 0f;
        var hitSet = new HashSet<Collider2D>();
        bool playerBounced = false;

        while (timer < downAttackHitboxDuration)
        {
            Vector2 origin = (Vector2)transform.position + diag * downAttackRange;

            // sample with layer mask
            Collider2D[] hits = Physics2D.OverlapBoxAll(origin, downAttackBoxSize, 0f, attackLayer);

            foreach (var hit in hits)
            {
                if (hit == null) continue;
                if (hitSet.Contains(hit)) continue;
                if (!IsValidTarget(hit)) continue;

                // apply enemy knockback
                int facing = playerMovement != null ? playerMovement.facingDirection : 1;
                Vector2 kb = new Vector2(facing * knockbackForce * 0.8f, -knockbackForce * 0.6f);
                HandleHit(hit, kb, false);
                hitSet.Add(hit);

                // bounce player (only once)
                if (!playerBounced && playerMovement != null && playerMovement.rb != null)
                {
                    playerMovement.ApplyVelocityLock(new Vector2(playerMovement.rb.linearVelocity.x, downAttackBounce), downAttackBounceLock);
                    playerBounced = true;
                }
            }

            timer += downAttackHitboxInterval;
            // draw for debugging briefly
            DebugDrawBox((Vector2)transform.position + diag * downAttackRange, downAttackBoxSize, Color.cyan, downAttackHitboxInterval);

            yield return new WaitForSeconds(downAttackHitboxInterval);
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
            HandleHit(hit, kb, false);
            Debug.Log("Side Hit: " + hit.name);
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
            HandleHit(hit, kb, true); // forceSetVelocity = true
            Debug.Log("Top Hit: " + hit.name);
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