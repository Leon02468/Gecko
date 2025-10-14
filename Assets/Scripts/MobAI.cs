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

    void Awake()
    {
        if (mobAnimation == null)
            mobAnimation = GetComponentInChildren<MobAnimation>();
    }

    void Update()
    {
        targetPlayer = FindPlayerInRange();

        if (targetPlayer != null)
        {
            // Face player immediately via transform (keeps physics/movement consistent)
            FaceTarget(targetPlayer.position);

            // If close enough and cooldown passed, attack
            float dist = Vector2.Distance(transform.position, targetPlayer.position);
            if (!isAttacking && Time.time >= lastAttackTime + attackCooldown && dist <= detectionRadius)
            {
                StartCoroutine(PerformAttack(targetPlayer));
            }
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
        Vector3 localScale = transform.localScale;
        float dir = Mathf.Sign(pos.x - transform.position.x);
        if (dir == 0) return;
        localScale.x = Mathf.Abs(localScale.x) * (dir < 0 ? -1f : 1f);
        transform.localScale = localScale;
    }

    private IEnumerator PerformAttack(Transform player)
    {
        if (player == null) yield break;

        isAttacking = true;
        lastAttackTime = Time.time;

        // compute facing towards player and pass it to animation so sprite flips before attack plays
        int facing = (player.position.x >= transform.position.x) ? 1 : -1;
        mobAnimation?.PlayAttack(facing);

        // wait until attack hits (animation timing)
        yield return new WaitForSeconds(attackDelay);

        if (player == null)
        {
            isAttacking = false;
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

        yield return new WaitForSeconds(0.02f);
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