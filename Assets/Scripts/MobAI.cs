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

    void Awake()
    {
        if (mobAnimation == null)
            mobAnimation = GetComponentInChildren<MobAnimation>();

        mobMovement = GetComponent<MobMovement>();
    }

    void Update()
    {
        // If mobAnimation was added later or is null for some instances, try to refresh reference each frame
        if (mobAnimation == null)
            mobAnimation = GetComponentInChildren<MobAnimation>();

        targetPlayer = FindPlayerInRange();

        if (targetPlayer != null)
        {
            // Face player immediately via animation driver when available (keeps flipping consistent)
            FaceTarget(targetPlayer.position);

            // If close enough and cooldown passed, attack
            float dist = Vector2.Distance(transform.position, targetPlayer.position);
            if (!isAttacking && Time.time >= lastAttackTime + attackCooldown && dist <= attackRange)
            {
                StartCoroutine(PerformAttack(targetPlayer));
            }
        }
        else
        {
            // No player detected: stop forcing facing so patrol movement or other systems can control it
            desiredFacing = 0;
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

    void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;
        Gizmos.color = detectColor;
        Gizmos.DrawSphere(transform.position, detectionRadius);
        Gizmos.color = attackColor;
        Gizmos.DrawSphere(transform.position, attackRange);
    }
}