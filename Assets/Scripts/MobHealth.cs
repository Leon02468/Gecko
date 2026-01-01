using Pathfinding;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider2D))]
public class MobHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField] private int maxHP = 3;
    [SerializeField] private bool destroyOnDeath = true;
    [SerializeField] private float deathDelay = 0.1f;

    [Header("Enemy Identification")]
    [Tooltip("The EnemyType ScriptableObject for this mob (used for mission tracking)")]
    public EnemyType enemyType; // NEW: Assign in Inspector

    [Header("Invulnerability")]
    [SerializeField] private bool useInvulnerability = true;
    [SerializeField] private float invulnerabilityDuration = 0.25f;

    [Header("Knockback Settings")]
    [Tooltip("Only allow knockback when mob is grounded (prevents juggling in air)")]
    [SerializeField] private bool onlyKnockbackWhenGrounded = true;
    [Tooltip("Layer mask for ground detection")]
    [SerializeField] private LayerMask groundLayer = ~0;
    [Tooltip("Distance to check for ground below the mob")]
    [SerializeField] private float groundCheckDistance = 0.1f;
    [Tooltip("Width of the ground check box (based on collider)")]
    [SerializeField] private float groundCheckWidth = 0.9f;

    [Header("Events")]
    public UnityEvent OnDamaged;
    public UnityEvent OnDead;

    public int CurrentHP { get; private set; }

    private bool isInvulnerable;
    private bool isGrounded;
    private Rigidbody2D rb;
    private Collider2D coll;
    private MonoBehaviour[] disableOnDeath;
    
    public bool IsGrounded => isGrounded;

    // Existing money drop (kept for compatibility)
    public GameObject moneyDropPrefab;
    public int moneyAmount = 1;

    void Awake()
    {
        CurrentHP = Mathf.Max(0, maxHP);
        rb = GetComponent<Rigidbody2D>();
        coll = GetComponent<Collider2D>();
        disableOnDeath = GetComponents<MonoBehaviour>();
    }

    void Update()
    {
        if (onlyKnockbackWhenGrounded && rb != null && coll != null)
        {
            CheckGrounded();
        }
    }

    private void CheckGrounded()
    {
        Bounds bounds = coll.bounds;
        Vector2 checkPosition = new Vector2(bounds.center.x, bounds.min.y);
        Vector2 boxSize = new Vector2(bounds.size.x * groundCheckWidth, groundCheckDistance);
        isGrounded = Physics2D.OverlapBox(checkPosition - Vector2.up * (groundCheckDistance * 0.5f), boxSize, 0f, groundLayer);
    }

    public void TakeDamage(int amount, Vector2? knockback = null)
    {
        if (amount <= 0) return;
        if (useInvulnerability && isInvulnerable) return;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayEnemyGetHit();

        CurrentHP -= amount;
        OnDamaged?.Invoke();

        if (knockback.HasValue)
            ApplyKnockback(knockback.Value);

        if (CurrentHP <= 0)
        {
            StartCoroutine(HandleDeath());
            return;
        }

        if (useInvulnerability)
            StartCoroutine(InvulnerabilityCoroutine(invulnerabilityDuration));
    }

    private IEnumerator ReenableAIPathAfterDelay(AIPath aiPath, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (aiPath != null)
            aiPath.enabled = true;
    }

    private void ApplyKnockback(Vector2 kb)
    {
        if (onlyKnockbackWhenGrounded && !isGrounded)
        {
            Debug.Log($"{gameObject.name}: Knockback blocked - mob is airborne");
            return;
        }

        if (rb != null)
        {
            var aiPath = GetComponent<Pathfinding.AIPath>();
            if (aiPath != null)
            {
                aiPath.enabled = false;
                rb.AddForce(kb, ForceMode2D.Impulse);
                Debug.Log($"{gameObject.name}: Knockback applied (grounded) - {kb}");
                StartCoroutine(ReenableAIPathAfterDelay(aiPath, 0.25f));
            }
            else
            {
                rb.AddForce(kb, ForceMode2D.Impulse);
                Debug.Log($"{gameObject.name}: Knockback applied (grounded) - {kb}");
            }
            return;
        }

        var pm = GetComponent<PlayerMovement>();
        if (pm != null)
        {
            pm.ApplyVelocityLock(kb, 0.12f);
            return;
        }

        transform.position += (Vector3)(kb * 0.02f);
    }

    private IEnumerator InvulnerabilityCoroutine(float duration)
    {
        isInvulnerable = true;
        yield return new WaitForSeconds(duration);
        isInvulnerable = false;
    }

    private IEnumerator HandleDeath()
    {
        OnDead?.Invoke();

        // NEW: Update mission progress for all missions tracking this enemy type
        if (enemyType != null)
        {
            UpdateMissionProgress();
        }
        else
        {
            Debug.LogWarning($"{gameObject.name} died but has no EnemyType assigned for mission tracking!");
        }

        if (coll != null) coll.enabled = false;
        foreach (var mb in disableOnDeath)
        {
            if (mb == this) continue;
            mb.enabled = false;
        }

        // Instantiate money drop
        if (moneyDropPrefab != null && moneyAmount > 0)
        {
            GameObject drop = Instantiate(moneyDropPrefab, transform.position, Quaternion.identity);
            var moneyDrop = drop.GetComponent<MoneyDrop>();
            if (moneyDrop != null)
            {
                moneyDrop.amount = moneyAmount;
            }
        }

        if (destroyOnDeath)
            Destroy(gameObject, deathDelay);
        else
            yield return new WaitForSeconds(deathDelay);
    }

    // NEW: Update all missions that track this enemy type
    private void UpdateMissionProgress()
    {
        if (MissionManager.Instance == null)
        {
            Debug.LogWarning("MissionManager.Instance is null! Cannot update mission progress.");
            return;
        }

        // Find all missions that track this enemy type
        var allMissions = MissionManager.Instance.allMissions;
        foreach (var mission in allMissions)
        {
            // Check if this mission tracks this enemy type
            if (mission.enemyType != null && mission.enemyType.id == enemyType.id)
            {
                // Update progress
                MissionManager.Instance.UpdateMissionProgress(mission.id, 1);
                Debug.Log($"Mission '{mission.id}' updated: killed {enemyType.displayName} ({mission.currentCount}/{mission.targetCount})");
            }
        }
    }

    public void Heal(int amount)
    {
        if (amount <= 0) return;
        CurrentHP = Mathf.Min(maxHP, CurrentHP + amount);
    }

    void OnDrawGizmosSelected()
    {
        if (!onlyKnockbackWhenGrounded) return;

        Collider2D col = coll != null ? coll : GetComponent<Collider2D>();
        if (col == null) return;

        Bounds bounds = col.bounds;
        Vector2 checkPosition = new Vector2(bounds.center.x, bounds.min.y);
        Vector2 boxSize = new Vector2(bounds.size.x * groundCheckWidth, groundCheckDistance);

        Gizmos.color = isGrounded ? Color.green : Color.red;
        Vector3 center = checkPosition - Vector2.up * (groundCheckDistance * 0.5f);
        Gizmos.DrawWireCube(center, boxSize);
    }
}