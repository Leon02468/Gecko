using UnityEngine;
using System.Collections;

public class BossController : MonoBehaviour
{
    public BossPhase[] phases;
    public int maxHP = 30;
    public Transform player;
    public LayerMask groundMask;

    [Header("Refs")]
    public Animator anim;
    public SpriteRenderer sr;
    public AudioSource audioSource;

    [Header("Tuning")]
    public float sightRange = 12f;
    public float facingFlipThreshold = 0.1f;

    int _hp;
    int _phaseIndex = 0;
    Rigidbody2D _rb;
    bool _busy;               // during attacks or hard transitions
    bool _dead;
    Vector2 _moveTarget;      // optional for simple repositioning

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _hp = maxHP;
        ApplyPhaseStats();
    }

    void Update()
    {
        if (_dead || player == null) return;

        // Update facing
        float dx = player.position.x - transform.position.x;
        if (Mathf.Abs(dx) > facingFlipThreshold)
            sr.flipX = dx < 0f;

        // Phase transition check
        TryPhaseTransition();

        if (!_busy)
            // simple decision loop by coroutine
            StartCoroutine(DecisionLoop());
    }

    IEnumerator DecisionLoop()
    {
        _busy = true;

        BossPhase phase = phases[_phaseIndex];

        // Simple spacing: move towards preferred range of a random attack
        var attack = phase.attacks[Random.Range(0, phase.attacks.Length)];
        float dist = Mathf.Abs(player.position.x - transform.position.x);

        if (dist > attack.preferredRange + 0.5f)
        {
            yield return MoveHorizontallyToward(player.position.x, phase.moveSpeed, attack.preferredRange);
        }
        else if (dist < attack.preferredRange - 0.5f)
        {
            yield return MoveHorizontallyToward(transform.position.x + (sr.flipX ? -1f : 1f) * 2f,
                                                phase.moveSpeed, attack.preferredRange);
        }

        // Execute attack
        yield return ExecuteAttack(attack);

        // Small idle gap
        yield return new WaitForSeconds(Random.Range(phase.minDecisionGap, phase.maxDecisionGap));
        _busy = false;
    }

    IEnumerator MoveHorizontallyToward(float targetX, float speed, float stopWithin = 0.5f)
    {
        float dir = Mathf.Sign(targetX - transform.position.x);
        float goalDist = Mathf.Abs(targetX - transform.position.x);
        float timeout = Mathf.Min(2.0f + goalDist / speed, 4f);

        float t = 0f;
        while (goalDist > stopWithin && t < timeout)
        {
            _rb.linearVelocity = new Vector2(dir * speed, _rb.linearVelocity.y);
            yield return null;
            goalDist = Mathf.Abs(targetX - transform.position.x);
            t += Time.deltaTime;
        }
        _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
    }

    IEnumerator ExecuteAttack(BossAttack atk)
    {
        // WINDUP
        anim.SetTrigger("Windup");
        Flash(atk.flashColor, atk.flashIntensity, atk.windup);
        PlaySafe(atk.sfxWindup);
        if (atk.lockFacingDuringAttack) LockFacing(true);
        yield return new WaitForSeconds(atk.windup);

        // ACTIVE
        anim.SetTrigger("Attack");
        PlaySafe(atk.sfxSwing);
        EnableHitbox(atk.hitboxName, true);

        // movement burst (dash/short hop)
        var v = _rb.linearVelocity;
        float xdir = sr.flipX ? -1f : 1f;
        _rb.linearVelocity = new Vector2(atk.dashVelocity.x * xdir, v.y + atk.dashVelocity.y);

        yield return new WaitForSeconds(atk.active);

        // RECOVERY
        EnableHitbox(atk.hitboxName, false);
        if (atk.lockFacingDuringAttack) LockFacing(false);
        anim.SetTrigger("Recover");
        yield return new WaitForSeconds(atk.recovery);
    }

    void Flash(Color c, float intensity, float duration)
    {
        // simple flash: change color for duration (you can swap to a shader later)
        StartCoroutine(FlashC(c, intensity, duration));
    }

    IEnumerator FlashC(Color c, float intensity, float duration)
    {
        Color original = sr.color;
        sr.color = Color.Lerp(original, c, intensity);
        yield return new WaitForSeconds(duration);
        sr.color = original;
    }

    void PlaySafe(AudioClip clip)
    {
        if (clip && audioSource) audioSource.PlayOneShot(clip);
    }

    void LockFacing(bool locked)
    {
        // Optionally disable facing updates; here we just stop flipping during attack by ignoring Update flip.
        // You can implement a bool gate; for brevity we skip.
    }

    void EnableHitbox(string childName, bool on)
    {
        if (string.IsNullOrEmpty(childName)) return;
        var t = transform.Find(childName);
        if (!t) return;
        var col = t.GetComponent<Collider2D>();
        if (col) col.enabled = on;
    }

    void TryPhaseTransition()
    {
        int hpPercent = Mathf.RoundToInt((_hp / (float)maxHP) * 100f);
        for (int i = 0; i < phases.Length; i++)
        {
            if (i > _phaseIndex && hpPercent <= phases[i].enterAtHPPercent)
            {
                _phaseIndex = i;
                ApplyPhaseStats();
                StartCoroutine(PhaseStagger());
                break;
            }
        }
    }

    IEnumerator PhaseStagger()
    {
        _busy = true;
        anim.SetTrigger("PhaseShift");
        // small invuln + FX window
        yield return new WaitForSeconds(1.0f);
        _busy = false;
    }

    void ApplyPhaseStats()
    {
        var p = phases[_phaseIndex];
        _rb.gravityScale = p.gravityScale;
    }

    // Public damage API (from player weapon or projectile)
    public void TakeDamage(int amount, Vector2 hitFrom, float knockback)
    {
        if (_dead) return;
        _hp -= amount;
        anim.SetTrigger("Hurt");
        // basic knockback
        float dir = Mathf.Sign(transform.position.x - hitFrom.x);
        _rb.linearVelocity = new Vector2(dir * knockback, _rb.linearVelocity.y + 3f);

        if (_hp <= 0) Die();
    }

    void Die()
    {
        _dead = true;
        _rb.linearVelocity = Vector2.zero;
        anim.SetTrigger("Die");
        // disable all hitboxes
        foreach (var col in GetComponentsInChildren<Collider2D>())
            if (col.isTrigger) col.enabled = false;
        // TODO: doors open, loot, cutscene, etc.
    }
}
