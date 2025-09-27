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

    [Header("Events")]
    public UnityEvent OnDamaged;
    public UnityEvent OnDead;

    public int CurrentHP { get; private set; }

    private bool isInvulnerable;
    private Rigidbody2D rb;
    private Collider2D coll;
    private MonoBehaviour[] disableOnDeath;

    void Awake()
    {
        CurrentHP = Mathf.Max(0, maxHP);
        rb = GetComponent<Rigidbody2D>();
        coll = GetComponent<Collider2D>();
        disableOnDeath = GetComponents<MonoBehaviour>();
    }

    public void TakeDamage(int amount, Vector2? knockback = null)
    {
        if (amount <= 0) return;
        if (useInvulnerability && isInvulnerable) return;

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

    private void ApplyKnockback(Vector2 kb)
    {
        if (rb != null)
        {
            switch (rb.bodyType)
            {
                case RigidbodyType2D.Dynamic:
                    rb.AddForce(kb, ForceMode2D.Impulse);
                    break;
                case RigidbodyType2D.Kinematic:
                    rb.linearVelocity = kb;
                    break;
                case RigidbodyType2D.Static:
                    transform.position += (Vector3)(kb * 0.02f);
                    break;
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
}