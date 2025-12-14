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

    [Header("Invulnerability")]
    [SerializeField] private bool useInvulnerability = true;
    [SerializeField] private float invulnerabilityDuration = 0.25f;

    [Header("Knockback Settings")]
    [Tooltip("Only allow knockback when mob is grounded (prevents juggling in air)")]
    [SerializeField] private bool onlyKnockbackWhenGrounded = true;
    [Tooltip("Layer mask for ground detection")]
    [SerializeField] private LayerMask groundLayer = ~0; // default to everything
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
        // Check if mob is grounded
        if (onlyKnockbackWhenGrounded && rb != null && coll != null)
        {
            CheckGrounded();
        }
    }

    private void CheckGrounded()
    {
        // Get the bounds of the collider
        Bounds bounds = coll.bounds;
        Vector2 checkPosition = new Vector2(bounds.center.x, bounds.min.y);
        Vector2 boxSize = new Vector2(bounds.size.x * groundCheckWidth, groundCheckDistance);

        // Check for ground using a box cast slightly below the mob
        isGrounded = Physics2D.OverlapBox(checkPosition - Vector2.up * (groundCheckDistance * 0.5f), boxSize, 0f, groundLayer);
    }

    public void TakeDamage(int amount, Vector2? knockback = null)
    {
        if (amount <= 0) return;
        if (useInvulnerability && isInvulnerable) return;

        // Play enemy get hit SFX
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
        // Check if knockback should be prevented when airborne
        if (onlyKnockbackWhenGrounded && !isGrounded)
        {
            Debug.Log($"{gameObject.name}: Knockback blocked - mob is airborne");
            return;
        }

        if (rb != null)
        {
            //Temporarily Disable AIPath so the any enemies have A* can have knockback
            var aiPath = GetComponent<Pathfinding.AIPath>();
            if (aiPath != null)
            {
                aiPath.enabled = false;
                rb.AddForce(kb, ForceMode2D.Impulse);
                Debug.Log($"{gameObject.name}: Knockback applied (grounded) - {kb}");
                StartCoroutine(ReenableAIPathAfterDelay(aiPath, 0.25f)); // Adjust delay as needed
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

    public void Heal(int amount)
    {
        if (amount <= 0) return;
        CurrentHP = Mathf.Min(maxHP, CurrentHP + amount);
    }

    // Visualize ground check in editor
    void OnDrawGizmosSelected()
    {
        if (!onlyKnockbackWhenGrounded) return;

        Collider2D col = coll != null ? coll : GetComponent<Collider2D>();
        if (col == null) return;

        Bounds bounds = col.bounds;
        Vector2 checkPosition = new Vector2(bounds.center.x, bounds.min.y);
        Vector2 boxSize = new Vector2(bounds.size.x * groundCheckWidth, groundCheckDistance);

        // Draw ground check box
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Vector3 center = checkPosition - Vector2.up * (groundCheckDistance * 0.5f);
        Gizmos.DrawWireCube(center, boxSize);
    }
}