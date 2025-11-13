using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class MantisBossController : MonoBehaviour
{
    [Header("Phases")]
    public BossPhase[] phases; // Phase1 then Phase2

    [Header("HP")]
    public int maxHP = 25;

    [Header("Refs")]
    public Transform player;
    public Animator anim;
    public AudioSource audioSource;

    [Header("Prefabs")]
    public GameObject vineProjectilePrefab;
    public Transform projectileSpawnPoint;

    [Header("Stretch / DOT")]
    public float stretchHoldTime = 3f;
    public float stretchDotInterval = 2f;
    public float stretchDotDamage = 0.5f;

    Rigidbody2D rb;
    int currentHP;
    int currentPhaseIndex = 0;
    bool busy = false;
    bool dead = false;

    int GetFacingSign()
    {
        return transform.localScale.x >= 0 ? -1 : 1;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        currentHP = maxHP;
        ApplyPhaseStats();
    }

    void Update()
    {
        Debug.Log("Facing: " + GetFacingSign());
        if (dead || player == null) return;

        if (!busy)
        {
            UpdateFacing();
            TryPhaseTransition();
            StartCoroutine(DecisionLoop());
        }
    }

    void UpdateFacing()
    {
        float dx = transform.position.x - player.position.x;
        if (Mathf.Abs(dx) < 0.1f) return;
        int facingSign = dx > 0f ? 1 : -1;        // +1 => facing left(no rotate), -1 => right(rotate)
        var s = transform.localScale;
        s.x = Mathf.Abs(s.x) * facingSign;
        transform.localScale = s;
    }

    void TryPhaseTransition()
    {
        if (currentPhaseIndex + 1 >= phases.Length) return;
        // you asked phase change at 13 HP left; use exact HP check
        if (currentHP <= 13 && currentPhaseIndex == 0)
        {
            currentPhaseIndex = 1;
            ApplyPhaseStats();
            StartCoroutine(PhaseShiftStagger());
        }
    }

    void ApplyPhaseStats()
    {
        if (phases == null || phases.Length == 0) return;
        var p = phases[Mathf.Clamp(currentPhaseIndex, 0, phases.Length - 1)];
        rb.gravityScale = p.gravityScale;
    }

    IEnumerator PhaseShiftStagger()
    {
        busy = true;
        anim.SetTrigger("PhaseShift");
        yield return new WaitForSeconds(1f);
        busy = false;
    }

    IEnumerator DecisionLoop()
    {
        busy = true;
        var phase = phases[Mathf.Clamp(currentPhaseIndex, 0, phases.Length - 1)];
        var atk = phase.attacks[Random.Range(0, phase.attacks.Length)];

        // spacing
        float dist = Mathf.Abs(player.position.x - transform.position.x);
        if (dist > atk.preferredRange + 0.5f)
        {
            yield return MoveHorizontally(player.position.x, phase.moveSpeed, atk.preferredRange);
        }
        else if (dist < atk.preferredRange - 0.5f)
        {
            float stepBackDistance = 2.0f;
            float awayDir = Mathf.Sign(transform.position.x - player.position.x);
            float targetX = transform.position.x + awayDir * stepBackDistance;
            yield return MoveHorizontally(targetX, phase.moveSpeed, 0.6f);
        }

        // execute
        yield return StartCoroutine(ExecuteAttack(atk));
        // decision gap
        yield return new WaitForSeconds(Random.Range(phase.minDecisionGap, phase.maxDecisionGap));
        busy = false;
    }

    IEnumerator MoveHorizontally(float targetX, float speed, float stopWithin)
    {
        float timeout = 3f;
        float t = 0f;
        while (Mathf.Abs(targetX - transform.position.x) > stopWithin && t < timeout)
        {
            float dir = Mathf.Sign(targetX - transform.position.x);
            rb.linearVelocity = new Vector2(dir * speed, rb.linearVelocity.y);
            yield return null;
            t += Time.deltaTime;
        }
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    IEnumerator ExecuteAttack(BossAttack atk)
    {
        // windup
        if(!string.IsNullOrEmpty(atk.windupTrigger))
            anim.SetTrigger(atk.windupTrigger);
        else
            anim.SetTrigger("Windup");
        
        PlaySfx(atk); // placeholder
        if (atk.lockFacingDuringAttack) { /* keep facing */ }
        yield return new WaitForSeconds(atk.windup);

        // ACTIVE behavior based on hitboxName
        if(!string.IsNullOrEmpty(atk.attackTrigger))
            anim.SetTrigger(atk.attackTrigger);
        else
            anim.SetTrigger("Attack");  

        if (atk.hitboxName == "VINE_PROJECTILE")
        {
            // spawn projectiles
            SpawnVineProjectiles(atk);
            yield return new WaitForSeconds(atk.active);
        }
        else if (atk.hitboxName == "Hitboxes/ClawFront")
        {
            // two quick pulses
            var hb = FindHitbox(atk.hitboxName);
            var hbScript = hb?.GetComponent<MantisHitbox>();
            if (hbScript != null)
            {
                hbScript.damage = atk.damage;
                for (int i = 0; i < 2; i++)
                {
                    hb.gameObject.SetActive(true);
                    yield return new WaitForSeconds(0.5f);
                    hb.gameObject.SetActive(false);
                    yield return new WaitForSeconds(0.03f);
                }
            }
            else
            {
                yield return new WaitForSeconds(atk.active);
            }
        }
        else if (atk.hitboxName == "Hitboxes/StretchHead")
        {
            var hb = FindHitbox(atk.hitboxName);
            var hbScript = hb?.GetComponent<MantisHitbox>();
            if (hbScript != null)
            {
                // lunge forward
                int facing = GetFacingSign();
                rb.linearVelocity = new Vector2(6f * facing, rb.linearVelocity.y);

                hbScript.enabled = true;
                hbScript.damage = atk.damage;
                yield return new WaitForSeconds(atk.active);
                // check overlap
                Collider2D hit = Physics2D.OverlapCircle(hbScript.transform.position, 0.3f, LayerMask.GetMask("Player"));
                if (hit)
                {
                    var ph = hit.GetComponent<PlayerHealth>();
                    if (ph != null)
                    {
                        // initial damage
                        ph.TakeDamage(atk.damage, transform.position, 0f);
                        // start grab + DOT controlled by this boss
                        ph.ApplyGrab(stretchHoldTime, () => { /* on released callback � nothing here */ });
                        StartCoroutine(StretchDotCoroutine(ph, stretchHoldTime));
                    }
                }
                hbScript.enabled = false;
            }
            else
            {
                yield return new WaitForSeconds(atk.active);
            }
        }
        else
        {
            // default: enable hitbox for duration
            var hb = FindHitbox(atk.hitboxName);
            var hbScript = hb?.GetComponent<MantisHitbox>();
            if (hbScript != null)
            {
                hbScript.damage = atk.damage;
                hbScript.enabled = true;
                // movement burst
                int facing = GetFacingSign();
                rb.linearVelocity = new Vector2(atk.dashVelocity.x * facing, rb.linearVelocity.y + atk.dashVelocity.y);
                yield return new WaitForSeconds(atk.active);
                hbScript.enabled = false;
            }
            else
            {
                yield return new WaitForSeconds(atk.active);
            }
        }

        // recovery
        yield return new WaitForSeconds(atk.recovery);
    }

    void SpawnVineProjectiles(BossAttack atk)
    {
        if (vineProjectilePrefab == null || projectileSpawnPoint == null) return;
        int count = (currentPhaseIndex >= 1) ? 3 : 1;
        float[] anglesLocal = (count == 0) ? new float[] { 0f } : new float[] { 0f, 30f, 60f };
        int facing = GetFacingSign();
        float baseAngle = (facing == 1) ? 0f : 180f;  //left:0, right:180

        foreach (var a in anglesLocal)
        {
            float final = baseAngle + a * facing;
            var go = Instantiate(vineProjectilePrefab, projectileSpawnPoint.position, Quaternion.Euler(0, 0, final));
            var vp = go.GetComponent<VineProjectile>();
            if (vp != null)
            {
                vp.damage = atk.damage;
                vp.Launch(final);
            }
        }
    }

    IEnumerator StretchDotCoroutine(PlayerHealth ph, float hold)
    {
        float elapsed = 0f;
        while (elapsed < hold && ph != null && ph.IsAlive)
        {
            yield return new WaitForSeconds(stretchDotInterval);
            ph.TakeDamage(stretchDotDamage, transform.position, 0f);
            elapsed += stretchDotInterval;
        }
    }

    Collider2D FindHitbox(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        var t = transform.Find(path);
        return t?.GetComponent<Collider2D>();
    }

    void PlaySfx(BossAttack a)
    {
        if (audioSource && a != null) { /* audioSource.PlayOneShot(a.sfxWindup) if exists */ } 
    }

    // Damage API from player's weapon/projectile should call this
    public void TakeDamageFromPlayer(float dmg)
    {
        if (dead) return;
        currentHP -= Mathf.RoundToInt(dmg); // boss HP is integer by spec
        if (currentHP <= 0) Die();
    }

    void Die()
    {
        dead = true;
        rb.linearVelocity = Vector2.zero;
        anim.SetTrigger("Die");
        // disable hitboxes
        foreach (var c in GetComponentsInChildren<Collider2D>()) if (c.isTrigger) c.enabled = false;
        // open door / spawn loot etc.
    }
}
