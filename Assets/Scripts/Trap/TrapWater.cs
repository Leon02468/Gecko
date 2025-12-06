using UnityEngine;

public class TrapWater : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            var player = other.GetComponent<PlayerHealth>();
            if (player != null)
            {
                player.TakeDamage(1);
                player.ForceRespawn(0.4f); //delay for hurt animation
            }
        }
    }
}
