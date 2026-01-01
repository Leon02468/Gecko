using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerMovement))]
public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField] private float maxHP = 5;
    [SerializeField] private bool destroyOnDeath = false;

    [Header("Invulnerability")]
    [SerializeField] private bool useInvulnerability = true;
    [SerializeField] private float invulnerabilityDuration = 0.75f;

    [Header("Knockback")]
    [Tooltip("Duration passed to PlayerMovement.ApplyVelocityLock when knockback is applied.")]
    [SerializeField] private float velocityLockDuration = 0.12f;

    [Header("Grab / Escape")]
    [SerializeField] private bool allowButtonMashToEscape = true;
    [SerializeField] private int mashCountToEscape = 8;
    [SerializeField] private InputActionReference mashActionRef;
    private int mashCount = 0;
    private bool isGrabbed = false;

    [Header("Events")]
    public UnityEvent OnDamaged;
    public UnityEvent OnDead;

    public float CurrentHP { get; private set; }

    private bool isInvulnerable;
    private PlayerMovement playerMovement;
    private PlayerAnimation playerAnimation;
    private Coroutine invulCor;
    private Coroutine grabCor;

    void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        playerAnimation = GetComponent<PlayerAnimation>();
        CurrentHP = Mathf.Max(0f, maxHP);
    }

    public void SetHealth(float hp)
    {
        CurrentHP = Mathf.Clamp(hp, 0f, maxHP);
    }

    //Back up if need to use old code
    //This line to make sure other classes use this method works normally
    public void TakeDamage(int amount, Vector2? knockback = null)
    {
        TakeDamage((float)amount, knockback ?? null, 0f);
    }

    public void TakeDamage(float amount, Vector2? knockbackVector = null, float? optionalKnockbackMagnitude = null)
    {
        if (amount <= 0f) return;
        if (useInvulnerability && isInvulnerable) return;

        CurrentHP -= amount;
        OnDamaged?.Invoke();

        // Play hurt SFX
        AudioManager.Instance?.PlayPlayerGetHit();

        // Play hurt animation (if available)
        playerAnimation?.PlayHurt();

        // Apply knockback:
        ApplyKnockback(knockbackVector, optionalKnockbackMagnitude);

        if (CurrentHP <= 0f)
        {
            Die();
            return;
        }

        if (useInvulnerability)
        {
            if (invulCor != null) StopCoroutine(invulCor);
            invulCor = StartCoroutine(InvulnerabilityCoroutine(invulnerabilityDuration));
        }
    }

    void ApplyKnockback(Vector2? knockbackVector, float? optionalMagnitude, Transform attacker = null)
    {
        // Prefer PlayerMovement.ApplyVelocityLock if available
        if (knockbackVector.HasValue)
        {
            Vector2 kv = knockbackVector.Value;
            if(playerMovement != null)
            {
                playerMovement.ApplyVelocityLock(kv, velocityLockDuration);
                return;
            }

            // fallback apply on rigidbody
            var rb = GetComponent<Rigidbody2D>();
            if (rb == null) return;

            Vector2 clamped = Vector2.ClampMagnitude(kv, playerMovement != null ? playerMovement.maxVelocityMagnitude : 20f);
            if (rb.bodyType == RigidbodyType2D.Dynamic)
                rb.linearVelocity = clamped;
            else if (rb.bodyType == RigidbodyType2D.Kinematic)
                rb.linearVelocity = clamped;
            else // static fallback
                transform.position += (Vector3)(clamped * 0.02f);

            return;
        }

        // If only magnitude provided, compute away-from-attacker direction
        if (optionalMagnitude.HasValue)
        {
            var rb = GetComponent<Rigidbody2D>();
            if (rb == null) return;

            // attacker may be null; try to infer direction from localScale as fallback
            float sign = transform.localScale.x >= 0f ? 1f : -1f; // fallback
            if (attacker != null)
                sign = Mathf.Sign(transform.position.x - attacker.position.x);

            Vector2 kv = new Vector2(sign * optionalMagnitude.Value, 3f); // small y kick
            if (playerMovement != null)
            {
                playerMovement.ApplyVelocityLock(kv, velocityLockDuration);
            }
            else
            {
                kv = Vector2.ClampMagnitude(kv, playerMovement != null ? playerMovement.maxVelocityMagnitude : 20f);
                if (rb.bodyType == RigidbodyType2D.Dynamic) rb.linearVelocity = kv;
                else if (rb.bodyType == RigidbodyType2D.Kinematic) rb.linearVelocity = kv;
            }
        }
    }

    public void Heal(float amount)
    {
        if (amount <= 0f) return;
        CurrentHP = Mathf.Min(maxHP, CurrentHP + amount);
    }

    private IEnumerator InvulnerabilityCoroutine(float duration)
    {
        isInvulnerable = true;
        yield return new WaitForSeconds(duration);
        isInvulnerable = false;
    }

    /// <summary>
    /// Manually start invincibility frames without taking damage.
    /// Useful for attacks or abilities that should grant temporary protection.
    /// </summary>
    public void StartInvincibility(float duration)
    {
        if (!useInvulnerability) return;
        
        if (invulCor != null) StopCoroutine(invulCor);
        invulCor = StartCoroutine(InvulnerabilityCoroutine(duration));
    }

    private void Die()
    {
        OnDead?.Invoke();
        if (destroyOnDeath)
            Destroy(gameObject);
        else
        {
            // Disable movement while respawning
            var pm = GetComponent<PlayerMovement>();
            if (pm != null) pm.enabled = false;

            // Start respawn coroutine
            StartCoroutine(RespawnWithLoading(0.4f));
        }
    }

    public void ForceRespawn(float delayBeforeLoading = 0.4f)
    {
        StartCoroutine(RespawnWithLoading(delayBeforeLoading));
    }

    private IEnumerator RespawnWithLoading(float delayBeforeLoading)
    {
       
        // Wait for the hurt animation to play
        yield return new WaitForSeconds(delayBeforeLoading);

        // Show loading screen (if available)
        if (SceneFader.Instance != null) yield return SceneFader.Instance.FadeOutRoutine();

        // Wait for loading screen duration
        yield return new WaitForSeconds(0.5f);

        // Respawn at checkpoint
        var pm = GetComponent<PlayerMovement>();
        if (pm != null)
        {   
            // Decide where to respawn
          
            Vector3 respawnPos = PlayerCheckpointManager.Instance.GetCheckpoint();
            pm.transform.position = respawnPos;
            pm.enabled = true;

            pm.transform.position = respawnPos;
            pm.enabled = true;
        }

        // Hide loading screen (if available)
        if (SceneFader.Instance != null) yield return SceneFader.Instance.FadeInRoutine();

        // Restore HP to at least 1
        //CurrentHP = Mathf.Max(1f, maxHP);
    }


    // --- Grab API ---
    // Called by boss when player is grabbed. holdDuration = how long boss holds, dotInterval/dotDamage handled by boss or this.
    public void ApplyGrab(float holdDuration, System.Action onReleased = null)
    {
        if (grabCor != null) StopCoroutine(grabCor);
        grabCor = StartCoroutine(GrabCoroutine(holdDuration, onReleased));
    }

    private IEnumerator GrabCoroutine(float holdDuration, System.Action onReleased)
    {
        // disable movement if PlayerMovement supports it
        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }

        mashCount = 0;
        isGrabbed = true;

        InputAction action = mashActionRef != null ? mashActionRef.action : null;
        bool actionEnableByUs = false;

        System.Action<InputAction.CallbackContext> performedHandler = ctx =>
        {
            mashCount++;
        };

        if(action != null)
        {
            //subcribe
            action.performed += performedHandler;
            //enable if not already
            if (!action.enabled)
            {
                action.Enable();
                actionEnableByUs = true;
            }
        }
        else
        {
            Debug.LogWarning("PlayerHealth.ApplyGrab: mashActionRef is not set, cannot mash to escape.");
        }

        float elapsed = 0f;
        while (elapsed < holdDuration)
        {
            //This condition is an double check if input system not working
            if (action == null)
            {
                if(Keyboard.current != null && Keyboard.current.zKey.wasPressedThisFrame)
                {
                    mashCount++;
                }
            }

            if (mashCount >= mashCountToEscape) break;

            elapsed += Time.deltaTime;
            yield return null;
        }

        // cleanup subscription
        if(action != null)
        {
            action.performed -= performedHandler;
            if (actionEnableByUs)
            {
                action.Disable();
            }
        }

        isGrabbed = false;
        // re-enable movement after short delay
        if (playerMovement != null)
        {
            yield return new WaitForSeconds(0.08f);
            playerMovement.enabled = true;
        }

        // notify caller (boss)
        onReleased?.Invoke();

        grabCor = null;
    }

    // small helper to check alive
    public bool IsAlive => CurrentHP > 0f;
}