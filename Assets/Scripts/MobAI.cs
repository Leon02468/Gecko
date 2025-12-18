using System.Collections;
using UnityEngine;

/// <summary>
/// Simple mob AI: detects the player, faces them, and performs a timed attack (animation + hit).
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class MobAI : MonoBehaviour
{
    [Header("Detection")]
    public float detectionRadius = 6f;
    public LayerMask playerLayer;
    public string playerTag = "Player";

    [Header("Attack")]
    public float attackRange = 1.2f;
    public float attackDelay = 0.25f;
    public float attackCooldown = 1.0f;
    public int attackDamage = 1;

    [Header("Charge Attack")]
    [Tooltip("Enable charge/thrust attack instead of basic melee")]
    public bool useChargeAttack = true;
    [Tooltip("Time to charge up before thrusting")]
    public float chargeUpTime = 0.3f;
    [Tooltip("Horizontal speed during thrust")]
    public float thrustSpeed = 8f;
    [Tooltip("Upward/jump force during thrust (0 = no jump, higher = more vertical)")]
    public float thrustJumpForce = 5f;
    [Tooltip("Duration of the thrust movement")]
    public float thrustDuration = 0.4f;
    [Tooltip("Minimum distance from target to trigger jump attack (between attackRange and detectionRadius)")]
    public float jumpAttackMinDistance = 2.0f;
    [Tooltip("Maximum distance from target to trigger jump attack (must be <= detectionRadius)")]
    public float jumpAttackMaxDistance = 4.5f;
    [Tooltip("Lock player's position during charge (thrust toward last known position)")]
    public bool lockTargetDuringCharge = true;

    [Header("Counter-Attack (Interrupt)")]
    [Tooltip("Can player interrupt the charge attack by attacking during thrust?")]
    public bool allowCounterAttack = true;
    [Tooltip("Knockback multiplier when counter-attacked (applied to mob)")]
    public float counterAttackKnockbackMultiplier = 1.5f;
    [Tooltip("Stun duration after being counter-attacked")]
    public float counterAttackStunDuration = 0.5f;

    [Header("Knockback")]
    public float knockbackForce = 6f;
    public float knockbackUpMultiplier = 0.4f;

    [Header("References")]
    public MobAnimation mobAnimation;

    [Header("Gizmos")]
    public bool drawGizmos = true;
    public Color detectColor = new Color(1f, 0.6f, 0.0f, 0.15f);
    public Color attackColor = new Color(1f, 0f, 0f, 0.2f);

    private float lastAttackTime = -Mathf.Infinity;
    private Transform targetPlayer;
    private bool isAttacking = false;

    // desired facing set by AI (1 = right, -1 = left). Applied in LateUpdate to override other components.
    // When zero, AI does not force facing and lets other components (e.g. patrol movement) control it.
    private int desiredFacing = 0;

    // reference to movement component so we can pause movement while attacking
    private MobMovement mobMovement;
    private Rigidbody2D rb;
    
    // Charge attack state
    private bool isCharging = false;
    private Vector3 chargeTargetPosition;
    
    // Counter-attack state
    private bool chargeInterrupted = false;
    private bool isStunned = false;
    public bool IsChargingAndVulnerable => isCharging && allowCounterAttack && !chargeInterrupted;

    void Awake()
    {
        if (mobAnimation == null)
            mobAnimation = GetComponentInChildren<MobAnimation>();

        mobMovement = GetComponent<MobMovement>();
        rb = GetComponent<Rigidbody2D>();
        
        // Validate jump attack distances
        jumpAttackMinDistance = Mathf.Max(jumpAttackMinDistance, attackRange);
        jumpAttackMaxDistance = Mathf.Min(jumpAttackMaxDistance, detectionRadius);
        
        // Ensure max >= min
        if (jumpAttackMaxDistance < jumpAttackMinDistance)
        {
            Debug.LogWarning($"{gameObject.name}: jumpAttackMaxDistance ({jumpAttackMaxDistance}) < jumpAttackMinDistance ({jumpAttackMinDistance}). Swapping values.");
            float temp = jumpAttackMaxDistance;
            jumpAttackMaxDistance = jumpAttackMinDistance;
            jumpAttackMinDistance = temp;
        }
        
        //// Debug component detection
        Debug.Log($"<color=cyan>===== {gameObject.name} MobAI initialized =====</color>");
        Debug.Log($"  - MobMovement: {(mobMovement != null ? "<color=green>Found</color>" : "<color=red>NOT FOUND</color>")}");
        Debug.Log($"  - Rigidbody2D: {(rb != null ? "<color=green>Found</color>" : "<color=red>NOT FOUND</color>")}");
        Debug.Log($"  - MobAnimation: {(mobAnimation != null ? "<color=green>Found</color>" : "<color=red>NOT FOUND</color>")}");
        Debug.Log($"  - Charge Attack Enabled: <color=yellow>{useChargeAttack}</color>");
        Debug.Log($"  - Jump Attack Range: <color=yellow>{jumpAttackMinDistance} to {jumpAttackMaxDistance}</color>");
        
        if (rb != null)
        {
            Debug.Log($"  - Rigidbody2D Body Type: <color=yellow>{rb.bodyType}</color>");
            Debug.Log($"  - Rigidbody2D Constraints: <color=yellow>{rb.constraints}</color>");

            // Warn if rigidbody is kinematic or has frozen position
            if (rb.bodyType == RigidbodyType2D.Kinematic)
            {
                Debug.LogWarning($"<color=orange>{gameObject.name}: Rigidbody2D is Kinematic - charge thrust may not work! Change to Dynamic.</color>");
            }

            if ((rb.constraints & RigidbodyConstraints2D.FreezePositionX) != 0)
            {
                Debug.LogWarning($"<color=orange>{gameObject.name}: Rigidbody2D has X position frozen - thrust won't work! Unfreeze X position.</color>");
            }
        }

        // Check for conflicting AI scripts
        var aiPath = GetComponent<Pathfinding.AIPath>();
        if (aiPath != null)
        {
            Debug.LogWarning($"<color=orange>{gameObject.name}: AIPath component found! This may conflict with charge attack. Consider disabling it during attack.</color>");
        }
    }

    void Update()
    {
        // If mobAnimation was added later or is null for some instances, try to refresh reference each frame
        if (mobAnimation == null)
            mobAnimation = GetComponentInChildren<MobAnimation>();

        // Don't update AI if stunned
        if (isStunned) return;

        targetPlayer = FindPlayerInRange();

        if (targetPlayer != null)
        {
            // Face player immediately via animation driver when available (keeps flipping consistent)
            FaceTarget(targetPlayer.position);

            // If close enough and cooldown passed, attack
            float dist = Vector2.Distance(transform.position, targetPlayer.position);
            
            // Determine if player is in jump attack range
            bool inJumpRange = useChargeAttack && dist >= jumpAttackMinDistance && dist <= jumpAttackMaxDistance;
            bool inMeleeRange = dist <= attackRange;
            
            if (!isAttacking && !isCharging && Time.time >= lastAttackTime + attackCooldown)
            {
                if (inJumpRange)
                {
                    // Player is in the jump attack zone - perform leap attack
                    Debug.Log($"<color=cyan>{gameObject.name}: Player in JUMP RANGE ({dist:F2} units) - triggering leap attack!</color>");
                    StartCoroutine(PerformChargeAttack(targetPlayer));
                }
                else if (inMeleeRange)
                {
                    // Player is very close - use melee attack
                    Debug.Log($"<color=cyan>{gameObject.name}: Player in MELEE RANGE ({dist:F2} units) - using melee attack!</color>");
                    StartCoroutine(PerformAttack(targetPlayer));
                }
                // If player is between detection and jump range, just track them (move closer via patrol)
            }
        }
        else
        {
            // No player detected: stop forcing facing so patrol movement or other systems can control it
            desiredFacing = 0;
        }
    }
    
    // Public method to manually trigger charge attack for testing
    // Call this from Unity Inspector button or another script
    [ContextMenu("Test Charge Attack")]
    public void TestChargeAttack()
    {
        var player = GameObject.FindWithTag(playerTag);
        if (player != null)
        {
            Debug.Log($"<color=red>===== MANUAL CHARGE ATTACK TRIGGER =====</color>");
            StartCoroutine(PerformChargeAttack(player.transform));
        }
        else
        {
            Debug.LogError($"Cannot trigger manual attack - no player with tag '{playerTag}' found!");
        }
    }

    void LateUpdate()
    {
        // Apply desired facing here so it wins over other components that might run in FixedUpdate/Update
        if (desiredFacing == 0) return;

        int dir = desiredFacing;

        if (mobAnimation != null)
        {
            // ensure animation-facing applied
            try { mobAnimation.SetFacing(dir); } catch { }

            // apply flip to all SpriteRenderers under mob so visuals consistently match
            var allSrs = GetComponentsInChildren<SpriteRenderer>(true);
            bool flipAll = dir < 0;
            bool useFlip = false;
            try { useFlip = mobAnimation.useSpriteFlip; } catch { useFlip = false; }

            foreach (var srAll in allSrs)
            {
                if (srAll == null) continue;
                if (useFlip)
                    srAll.flipX = flipAll;
                else
                {
                    var t = srAll.transform;
                    Vector3 ls = t.localScale;
                    ls.x = Mathf.Abs(ls.x) * (dir < 0 ? -1f : 1f);
                    t.localScale = ls;
                }
            }

            // keep root scale consistent
            Vector3 rootScale = transform.localScale;
            rootScale.x = Mathf.Abs(rootScale.x) * (dir < 0 ? -1f : 1f);
            transform.localScale = rootScale;
        }
        else
        {
            Vector3 localScale = transform.localScale;
            localScale.x = Mathf.Abs(localScale.x) * (dir < 0 ? -1f : 1f);
            transform.localScale = localScale;
        }
    }

    private Transform FindPlayerInRange()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRadius, playerLayer);
        Transform nearest = null;
        float best = float.MaxValue;

        foreach (var c in hits)
        {
            if (c == null) continue;
            if (c.transform.IsChildOf(transform) || c.gameObject == gameObject) continue;

            float d = Vector2.Distance(transform.position, c.transform.position);
            if (d < best)
            {
                best = d;
                nearest = c.transform;
            }
        }

        if (nearest == null && !string.IsNullOrEmpty(playerTag))
        {
            var candidate = GameObject.FindWithTag(playerTag);
            if (candidate != null)
            {
                float d = Vector2.Distance(transform.position, candidate.transform.position);
                if (d <= detectionRadius)
                    nearest = candidate.transform;
            }
        }

        return nearest;
    }

    private void FaceTarget(Vector3 pos)
    {
        float raw = pos.x - transform.position.x;
        if (Mathf.Approximately(raw, 0f)) return;
        int dir = raw > 0f ? 1 : -1;

        // record desired facing and attempt immediate visual update; final application is in LateUpdate
        desiredFacing = dir;

        // Also try immediate small update so visuals are responsive in editor/play
        if (mobAnimation != null)
        {
            try { mobAnimation.SetFacing(dir); } catch { }
        }
    }

    private IEnumerator PerformAttack(Transform player)
    {
        if (player == null) yield break;

        isAttacking = true;
        lastAttackTime = Time.time;

        // disable autonomous movement while attacking
        mobMovement?.SetMovementEnabled(false);

        // compute facing towards player and pass it to animation so sprite flips before attack plays
        int facing = (player.position.x >= transform.position.x) ? 1 : -1;
        mobAnimation?.PlayAttack(facing);

        // wait until attack hits (animation timing)
        yield return new WaitForSeconds(attackDelay);

        if (player == null)
        {
            isAttacking = false;
            mobMovement?.SetMovementEnabled(true);
            yield break;
        }

        float dist = Vector2.Distance(transform.position, player.position);
        if (dist <= attackRange)
        {
            var dmg = player.GetComponentInParent<IDamageable>();
            Vector2 kbDir = (player.position.x >= transform.position.x) ? Vector2.right : Vector2.left;
            Vector2 kb = kbDir * knockbackForce + Vector2.up * (knockbackForce * knockbackUpMultiplier);

            if (dmg != null)
            {
                dmg.TakeDamage(attackDamage, kb);
            }
            else
            {
                player.SendMessage("TakeDamage", attackDamage, SendMessageOptions.DontRequireReceiver);
                player.SendMessage("ApplyKnockback", kb, SendMessageOptions.DontRequireReceiver);
            }
        }

        // short recovery tick before allowing movement again
        yield return new WaitForSeconds(0.02f);

        // re-enable movement when attack finished
        mobMovement?.SetMovementEnabled(true);
        isAttacking = false;
    }
    
    private IEnumerator PerformChargeAttack(Transform player)
    {
        if (player == null) yield break;

        //Debug.Log($"<color=lime>========== {gameObject.name}: CHARGE ATTACK STARTED ==========</color>");
        
        isCharging = true;
        isAttacking = true;
        chargeInterrupted = false;
        lastAttackTime = Time.time;

        // CRITICAL: Disable autonomous movement while attacking
        if (mobMovement != null)
        {
            Debug.Log($"<color=yellow>{gameObject.name}: Disabling MobMovement</color>");
            mobMovement.SetMovementEnabled(false);
        }
        else
        {
            Debug.LogWarning($"<color=orange>{gameObject.name}: No MobMovement to disable!</color>");
        }

        // compute facing towards player
        int facing = (player.position.x >= transform.position.x) ? 1 : -1;
        
        // Lock target position if configured
        if (lockTargetDuringCharge)
        {
            chargeTargetPosition = player.position;
        }

        Debug.Log($"<color=cyan>{gameObject.name}: Charge-up phase started. Target: {chargeTargetPosition}</color>");

        // Play attack animation during charge-up
        mobAnimation?.PlayAttack(facing);

        // Charge-up phase: mob pauses briefly
        if (rb != null)
        {
            // Stop all movement during charge
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            Debug.Log($"<color=green>{gameObject.name}: Velocity set to (0, {rb.linearVelocity.y}) during charge</color>");
        }
        else
        {
            Debug.LogWarning($"<color=orange>{gameObject.name}: No Rigidbody2D - Transform movement</color>");
        }
        
        yield return new WaitForSeconds(chargeUpTime);

        // Check if interrupted during charge-up
        if (chargeInterrupted)
        {
            Debug.Log($"<color=red>{gameObject.name}: Charge interrupted during charge-up!</color>");
            yield return HandleChargeInterrupt();
            yield break;
        }

        // Update target position if not locked
        if (!lockTargetDuringCharge && player != null)
        {
            chargeTargetPosition = player.position;
        }

        // Calculate thrust direction
        Vector2 thrustDirection = (chargeTargetPosition - transform.position).normalized;
        
        Debug.Log($"<color=magenta>========== {gameObject.name}: THRUST STARTED! ==========</color>");
        Debug.Log($"<color=magenta>  Direction: ({thrustDirection.x:F2}, {thrustDirection.y:F2})</color>");
        Debug.Log($"<color=magenta>  Speed: {thrustSpeed}</color>");
        Debug.Log($"<color=magenta>  Jump Force: {thrustJumpForce}</color>");
        Debug.Log($"<color=magenta>  Duration: {thrustDuration}</color>");

        // Ensure facing matches thrust direction
        facing = thrustDirection.x >= 0f ? 1 : -1;
        FaceTarget(chargeTargetPosition);

        // Thrust phase: mob leaps/dashes toward target
        float thrustElapsed = 0f;
        
        // Calculate thrust velocity with jump component
        Vector2 thrustVelocity = new Vector2(thrustDirection.x * thrustSpeed, thrustJumpForce);
        
        Debug.Log($"<color=magenta>  Thrust Velocity: ({thrustVelocity.x:F2}, {thrustVelocity.y:F2})</color>");
        
        bool hitPlayer = false;
        int frameCount = 0;
        
        // Apply initial jump velocity
        if (rb != null)
        {
            rb.linearVelocity = thrustVelocity;
            Debug.Log($"<color=green>{gameObject.name}: Initial jump applied: ({thrustVelocity.x:F2}, {thrustVelocity.y:F2})</color>");
            
            // Play jump SFX for ant
            var ant = GetComponent<AntCrawlingEnemy>();
            var mobHealth = GetComponent<MobHealth>();
            if (ant != null && mobHealth != null && mobHealth.IsGrounded)
            {
                ant.PlayJumpSfx();
            }

        }

        while (thrustElapsed < thrustDuration)
        {
            // Check if interrupted during thrust
            if (chargeInterrupted)
            {
                Debug.Log($"<color=red>{gameObject.name}: COUNTER-ATTACKED! Thrust interrupted!</color>");
                yield return HandleChargeInterrupt();
                yield break;
            }

            frameCount++;
            
            if (rb != null)
            {
                // Maintain horizontal velocity, let gravity handle vertical
                Vector2 oldVel = rb.linearVelocity;
                rb.linearVelocity = new Vector2(thrustVelocity.x, rb.linearVelocity.y);
                
                // Debug every 10 frames to avoid spam
                if (frameCount % 10 == 1)
                {
                    Debug.Log($"<color=yellow>Frame {frameCount}: Vel ({oldVel.x:F2}, {oldVel.y:F2}) → ({rb.linearVelocity.x:F2}, {rb.linearVelocity.y:F2})</color>");
                }
            }
            else
            {
                // Fallback: move via transform (parabolic arc simulation)
                float horizontalMove = thrustDirection.x * thrustSpeed * Time.deltaTime;
                float verticalMove = (thrustJumpForce - 9.81f * thrustElapsed) * Time.deltaTime; // Simple gravity
                transform.position += new Vector3(horizontalMove, verticalMove, 0f);
            }

            // Check for hits during thrust
            if (player != null)
            {
                float dist = Vector2.Distance(transform.position, player.position);
                if (dist <= attackRange && !hitPlayer)
                {
                    Debug.Log($"<color=lime>========== {gameObject.name}: HIT PLAYER! ==========</color>");
                    
                    var dmg = player.GetComponentInParent<IDamageable>();
                    Vector2 kbDir = thrustDirection;
                    Vector2 kb = kbDir * knockbackForce + Vector2.up * (knockbackForce * knockbackUpMultiplier);

                    if (dmg != null)
                    {
                        dmg.TakeDamage(attackDamage, kb);
                    }
                    else
                    {
                        player.SendMessage("TakeDamage", attackDamage, SendMessageOptions.DontRequireReceiver);
                        player.SendMessage("ApplyKnockback", kb, SendMessageOptions.DontRequireReceiver);
                    }
                    
                    hitPlayer = true;
                }
            }

            thrustElapsed += Time.deltaTime;
            yield return null;
        }

        Debug.Log($"<color=cyan>{gameObject.name}: Thrust complete. Frames: {frameCount}, Hit: {hitPlayer}</color>");

        // Stop thrust movement
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            Debug.Log($"<color=green>{gameObject.name}: Thrust stopped, velocity: {rb.linearVelocity}</color>");
        }

        // Short recovery before re-enabling movement
        yield return new WaitForSeconds(0.1f);

        // re-enable movement when attack finished
        if (mobMovement != null)
        {
            Debug.Log($"<color=yellow>{gameObject.name}: Re-enabling MobMovement</color>");
            mobMovement.SetMovementEnabled(true);
        }
        
        isCharging = false;
        isAttacking = false;
        chargeInterrupted = false;
        
        Debug.Log($"<color=lime>========== {gameObject.name}: CHARGE ATTACK COMPLETE ==========</color>");
    }

    /// <summary>
    /// Called when player attacks the mob during its charge attack.
    /// This interrupts the charge and applies knockback to the mob.
    /// </summary>
    public void InterruptChargeAttack(Vector2 knockbackFromPlayer, int damageFromPlayer = 0)
    {
        if (!IsChargingAndVulnerable)
        {
            Debug.Log($"<color=yellow>{gameObject.name}: Cannot interrupt - not charging or already interrupted</color>");
            return;
        }

        chargeInterrupted = true;
        Debug.Log($"<color=orange>========== {gameObject.name}: CHARGE INTERRUPTED BY PLAYER! ==========</color>");
        
        // Apply enhanced knockback
        Vector2 enhancedKnockback = knockbackFromPlayer * counterAttackKnockbackMultiplier;
        
        // Apply damage through IDamageable interface (without knockback, we'll handle that separately)
        if (damageFromPlayer > 0)
        {
            var health = GetComponent<IDamageable>();
            if (health != null)
            {
                // Apply damage only, no knockback (pass null/zero)
                health.TakeDamage(damageFromPlayer, null);
            }
        }
        
        // Apply knockback directly by setting velocity (normal knockback, not accumulating)
        if (rb != null)
        {
            // Temporarily disable AIPath if present to allow knockback
            var aiPath = GetComponent<Pathfinding.AIPath>();
            if (aiPath != null && aiPath.enabled)
            {
                aiPath.enabled = false;
                StartCoroutine(ReenableAIPathAfterDelay(aiPath, 0.3f));
            }
            
            // Set velocity directly for normal knockback
            rb.linearVelocity = enhancedKnockback;
            Debug.Log($"<color=orange>{gameObject.name}: Counter-attack knockback applied: {enhancedKnockback}</color>");
        }
    }
    
    private IEnumerator ReenableAIPathAfterDelay(Pathfinding.AIPath aiPath, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (aiPath != null)
            aiPath.enabled = true;
    }
    
    private IEnumerator HandleChargeInterrupt()
    {
        Debug.Log($"<color=orange>{gameObject.name}: Handling charge interrupt...</color>");

        // DON'T stop movement immediately - let the knockback physics play out
        // The knockback from InterruptChargeAttack will handle the velocity

        // Play hurt animation if available
        mobAnimation?.PlayHurt();
        
        // Stun the mob briefly (but don't freeze velocity - allow knockback to continue)
        isStunned = true;
        yield return new WaitForSeconds(counterAttackStunDuration);
        isStunned = false;
        
        // Re-enable movement after stun
        if (mobMovement != null)
        {
            mobMovement.SetMovementEnabled(true);
        }
        
        isCharging = false;
        isAttacking = false;
        chargeInterrupted = false;
        
        Debug.Log($"<color=orange>{gameObject.name}: Interrupt handled, mob recovering...</color>");
    }
    
    void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;
        
        // Detection radius (outermost - yellow/orange)
        Gizmos.color = detectColor;
        Gizmos.DrawSphere(transform.position, detectionRadius);
        
        // Attack range (innermost - red)
        Gizmos.color = attackColor;
        Gizmos.DrawSphere(transform.position, attackRange);
        
        // Jump attack range (middle zone - green/cyan)
        if (useChargeAttack)
        {
            // Draw the jump range zone as a ring between min and max
            Gizmos.color = new Color(0f, 1f, 0.5f, 0.15f); // Cyan/green for jump zone (filled)
            Gizmos.DrawSphere(transform.position, jumpAttackMaxDistance);
            
            // Draw wireframe circles for min and max jump distance
            Gizmos.color = new Color(0f, 1f, 0.5f, 0.8f); // Brighter cyan
            DrawWireCircle(transform.position, jumpAttackMinDistance, 32);
            
            Gizmos.color = new Color(0f, 0.8f, 1f, 0.8f); // Bright cyan for max
            DrawWireCircle(transform.position, jumpAttackMaxDistance, 32);
        }
    }
    
    // Helper method to draw wire circles (Unity doesn't have Gizmos.DrawWireCircle in 2D)
    private void DrawWireCircle(Vector3 center, float radius, int segments)
    {
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + new Vector3(radius, 0, 0);
        
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }
}