using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(PlayerMovement))]
public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField] private int maxHP = 5;
    [SerializeField] private bool destroyOnDeath = false;

    [Header("Invulnerability")]
    [SerializeField] private bool useInvulnerability = true;
    [SerializeField] private float invulnerabilityDuration = 0.75f;

    [Header("Knockback")]
    [Tooltip("Duration passed to PlayerMovement.ApplyVelocityLock when knockback is applied.")]
    [SerializeField] private float velocityLockDuration = 0.12f;

    [Header("Events")]
    public UnityEvent OnDamaged;
    public UnityEvent OnDead;

    public int CurrentHP { get; private set; }

    private bool isInvulnerable;
    private PlayerMovement playerMovement;
    private PlayerAnimation playerAnimation;

    void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        playerAnimation = GetComponent<PlayerAnimation>();
        CurrentHP = Mathf.Max(0, maxHP);
    }

    public void TakeDamage(int amount, Vector2? knockback = null)
    {
        if (amount <= 0) return;
        if (useInvulnerability && isInvulnerable) return;

        CurrentHP -= amount;
        OnDamaged?.Invoke();

        // Play hurt animation (if available)
        playerAnimation?.PlayHurt();

        // apply knockback: prefer PlayerMovement velocity lock for consistent player behavior
        if (knockback.HasValue && playerMovement != null)
        {
            playerMovement.ApplyVelocityLock(knockback.Value, velocityLockDuration);
        }
        else if (knockback.HasValue)
        {
            var rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                switch (rb.bodyType)
                {
                    case RigidbodyType2D.Dynamic:
                        rb.AddForce(knockback.Value, ForceMode2D.Impulse);
                        break;
                    case RigidbodyType2D.Kinematic:
                        rb.linearVelocity = knockback.Value;
                        break;
                    case RigidbodyType2D.Static:
                        transform.position += (Vector3)(knockback.Value * 0.02f);
                        break;
                }
            }
        }

        if (CurrentHP <= 0)
        {
            Die();
            return;
        }

        if (useInvulnerability)
            StartCoroutine(InvulnerabilityCoroutine(invulnerabilityDuration));
    }

    public void Heal(int amount)
    {
        if (amount <= 0) return;
        CurrentHP = Mathf.Min(maxHP, CurrentHP + amount);
    }

    private IEnumerator InvulnerabilityCoroutine(float duration)
    {
        isInvulnerable = true;
        yield return new WaitForSeconds(duration);
        isInvulnerable = false;
    }

    private void Die()
    {
        OnDead?.Invoke();
        if (destroyOnDeath)
            Destroy(gameObject);
        else
        {
            var pm = GetComponent<PlayerMovement>();
            if (pm != null) pm.enabled = false;
        }
    }

}