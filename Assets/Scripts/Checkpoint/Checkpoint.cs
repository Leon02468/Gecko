using Unity.Cinemachine;
using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerCheckpointManager checkpointManager = FindFirstObjectByType<PlayerCheckpointManager>();
            checkpointManager.SetCheckpoint(transform.position);
            Debug.Log($"Checkpoint set to {transform.position.x}:{transform.position.y}");
        }
    }
}
