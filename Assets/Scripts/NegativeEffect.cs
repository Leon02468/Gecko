using UnityEngine;

public class NegativeEffect : MonoBehaviour
{
    [Header("References")]
    public PlayerMovement playerMovement;

    private float originalSpeed;
    private bool isSlowed = false;
    private float slowDuration = 0f;

    private void Start()
    {
        originalSpeed = playerMovement.speed;
    }

    private void Update()
    {
        Debug.Log("CurrentSpeed: " + playerMovement.speed);
        if (isSlowed)
        {
            slowDuration -= Time.deltaTime;
            if (slowDuration <= 0f)
            {
                RemoveSlow();
            }
        }
    }

    public void ApplySlow(float slowMultiplier, float duration)
    {
        if (!isSlowed)
        {
            // first time slow applied
            originalSpeed = playerMovement.speed;
            playerMovement.speed *= slowMultiplier;
            isSlowed = true;
        }

        // refresh slow duration
        slowDuration = duration;
    }

    private void RemoveSlow()
    {
        playerMovement.speed = originalSpeed;
        isSlowed = false;
    }
}
