using UnityEngine;

public class waspZone : MonoBehaviour
{
    public EnemyShooter es;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Debug.Log("Have player");
            es.CheckPlayerInZone(true);
        }
            
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Debug.Log("No player");
            es.CheckPlayerInZone(false);
        }
    }
}
