using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class BossAI : MonoBehaviour
{
    [Header("Detection")]
    public float detectionRadius = 8f;
    public LayerMask playerLayer;
    public string playerTag = "Player";

    [Header("Attack selection")]
    public float meleeRange = 1.5f;
    public float jumpAttackHeightThreshold = 0.5f; // if player higher, may use jump attack
    [Range(0f,1f)] public float rangedChance = 0.25f; // chance to pick ranged when available

    [Header("Timings")]
    public float attackCooldown = 1.2f;
    public float meleeDelay = 0.18f;    // time from animation start to hit (fallback)
    public float jumpAttackDelay = 0.35f; // fallback
    public float rangedDelay = 0.28f; // fallback

    [Header("Damage")]
    public int meleeDamage = 2;
    public int jumpDamage = 3;
    public int rangedDamage = 1;

    [Header("Jump Attack Movement")]
    public float jumpForwardVelocity = 10f;
    public float jumpUpVelocity = 6f;

    [Header("Ranged")]
    public GameObject projectilePrefab;
    public Transform projectileSpawnPoint;
    public float projectileSpeed = 8f;

    [Header("Knockback")]
    public float knockbackForce = 6f;
    public float knockbackUpMultiplier = 0.4f;

    [Header("Animator (optional)")]
    public Animator animator;
    public string triggerMelee = "Melee";
    public string triggerJump = "JumpAttack";
    public string triggerRanged = "RangedAttack";
    public string paramSpeed = "Speed";
    public string paramVelY = "VelY";
    public string paramIsGrounded = "IsGrounded";

    [Header("Gizmos")]
    public bool drawGizmos = true;
    public Color detectColor = new Color(1f,0.6f,0f,0.15f);
    public Color attackColor = new Color(1f,0f,0f,0.2f);

    // runtime
    private Transform targetPlayer;
    private float lastAttackTime = -Mathf.Infinity;
    private bool isAttacking = false;
    private Rigidbody2D rb;

    // pending attack state used for animation events
    private Transform pendingTarget;
    private int pendingAttackType = 0; // 1=melee,2=jump,3=ranged
    private bool pendingHandled = false;

    void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        targetPlayer = FindPlayerInRange();
        UpdateAnimatorParams();

        if (targetPlayer == null) return;

        FaceTarget(targetPlayer.position);

        float dist = Vector2.Distance(transform.position, targetPlayer.position);

        if (!isAttacking && Time.time >= lastAttackTime + attackCooldown && dist <= detectionRadius)
        {
            int attackType = ChooseAttackType(targetPlayer, dist);
            StartCoroutine(PerformAttack(targetPlayer, attackType));
        }
    }

    private void UpdateAnimatorParams()
    {
        if (animator == null) return;
        float velX = rb != null ? rb.linearVelocity.x : 0f;
        float velY = rb != null ? rb.linearVelocity.y : 0f;
        animator.SetFloat(paramSpeed, Mathf.Abs(velX));
        animator.SetFloat(paramVelY, velY);
        // grounded parameter optional: if no ground checking, set true when vertical velocity near zero
        bool grounded = rb == null ? true : Mathf.Abs(rb.linearVelocity.y) < 0.05f;
        animator.SetBool(paramIsGrounded, grounded);
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
                best = d; nearest = c.transform;
            }
        }
        if (nearest == null && !string.IsNullOrEmpty(playerTag))
        {
            var candidate = GameObject.FindWithTag(playerTag);
            if (candidate != null)
            {
                float d = Vector2.Distance(transform.position, candidate.transform.position);
                if (d <= detectionRadius) nearest = candidate.transform;
            }
        }
        return nearest;
    }

    private int ChooseAttackType(Transform player, float dist)
    {
        if (player == null) return 1;
        // if player is above threshold relative to boss, do jump attack
        float vertical = player.position.y - transform.position.y;
        if (vertical > jumpAttackHeightThreshold)
            return 2; // jump attack

        // if projectile available and random chance, do ranged
        if (projectilePrefab != null && Random.value < rangedChance)
            return 3; // ranged

        // else melee
        return 1;
    }

    private void FaceTarget(Vector3 pos)
    {
        Vector3 localScale = transform.localScale;
        float dir = Mathf.Sign(pos.x - transform.position.x);
        if (dir == 0) return;
        localScale.x = Mathf.Abs(localScale.x) * (dir < 0 ? -1f : 1f);
        transform.localScale = localScale;
    }

    private IEnumerator PerformAttack(Transform player, int attackType)
    {
        if (player == null) yield break;
        isAttacking = true;
        lastAttackTime = Time.time;

        int facing = (player.position.x >= transform.position.x) ? 1 : -1;

        // set pending so animation event methods can act
        pendingTarget = player;
        pendingAttackType = attackType;
        pendingHandled = false;

        switch (attackType)
        {
            case 1: // melee
                animator?.SetTrigger(triggerMelee);
                // start fallback that executes if animation event doesn't fire
                StartCoroutine(FallbackPerform(meleeDelay));
                break;
            case 2: // jump attack
                animator?.SetTrigger(triggerJump);
                // apply forward/up velocity immediately if boss has rb
                if (rb != null)
                {
                    Vector2 v = new Vector2(facing * jumpForwardVelocity, jumpUpVelocity);
                    rb.linearVelocity = v;
                }
                StartCoroutine(FallbackPerform(jumpAttackDelay));
                break;
            case 3: // ranged
                animator?.SetTrigger(triggerRanged);
                StartCoroutine(FallbackPerform(rangedDelay));
                break;
        }

        // wait until either animation event or fallback handled the attack
        yield return new WaitUntil(() => pendingHandled == true);

        // short cooldown buffer
        yield return new WaitForSeconds(0.02f);
        pendingTarget = null;
        pendingAttackType = 0;
        pendingHandled = false;
        isAttacking = false;
    }

    // Fallback: called when animation events are not present. Executes action after delay.
    private IEnumerator FallbackPerform(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (pendingHandled) yield break;

        if (pendingAttackType == 3)
        {
            if (pendingTarget != null)
                SpawnProjectileAtPlayer(pendingTarget);
        }
        else if (pendingAttackType == 2)
        {
            if (pendingTarget != null)
                TryHitPlayer(pendingTarget, meleeRange, jumpDamage);
        }
        else // melee
        {
            if (pendingTarget != null)
                TryHitPlayer(pendingTarget, meleeRange, meleeDamage);
        }

        pendingHandled = true;
    }

    // These public methods are intended to be called from animation events at the hit/spawn frame.
    public void OnMeleeHit()
    {
        if (pendingHandled) return;
        if (pendingTarget == null) return;

        if (pendingAttackType == 2)
            TryHitPlayer(pendingTarget, meleeRange, jumpDamage);
        else
            TryHitPlayer(pendingTarget, meleeRange, meleeDamage);

        pendingHandled = true;
    }

    public void OnSpawnProjectile()
    {
        if (pendingHandled) return;
        if (pendingTarget == null) return;
        SpawnProjectileAtPlayer(pendingTarget);
        pendingHandled = true;
    }

    private void TryHitPlayer(Transform player, float range, int damage)
    {
        if (player == null) return;
        float d = Vector2.Distance(transform.position, player.position);
        if (d <= range)
        {
            var dmg = player.GetComponentInParent<IDamageable>();
            Vector2 kbDir = (player.position.x >= transform.position.x) ? Vector2.right : Vector2.left;
            Vector2 kb = kbDir * knockbackForce + Vector2.up * (knockbackForce * knockbackUpMultiplier);
            if (dmg != null)
                dmg.TakeDamage(damage, kb);
            else
            {
                player.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
                player.SendMessage("ApplyKnockback", kb, SendMessageOptions.DontRequireReceiver);
            }
        }
    }

    private void SpawnProjectileAtPlayer(Transform player)
    {
        if (projectilePrefab == null || projectileSpawnPoint == null || player == null) return;
        GameObject proj = Instantiate(projectilePrefab, projectileSpawnPoint.position, Quaternion.identity);
        // set velocity toward player if projectile has Rigidbody2D
        var prb = proj.GetComponent<Rigidbody2D>();
        Vector2 dir = (player.position - projectileSpawnPoint.position).normalized;
        if (prb != null)
        {
            prb.linearVelocity = dir * projectileSpeed;
        }
        // try to set damage on projectile if it has a component
        var projHit = proj.GetComponent<IProjectile>();
        if (projHit != null)
            projHit.Initialize(rangedDamage, dir * projectileSpeed);
    }

    void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;
        Gizmos.color = detectColor;
        Gizmos.DrawSphere(transform.position, detectionRadius);
        Gizmos.color = attackColor;
        Gizmos.DrawSphere(transform.position, meleeRange);
    }
}

// Optional projectile interface the boss will call if available on spawned prefab
public interface IProjectile
{
    void Initialize(int damage, Vector2 velocity);
}
