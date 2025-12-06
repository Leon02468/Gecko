using Unity.Cinemachine;
using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerCheckpointManager.Instance.SetCheckpoint(transform.position);
        }
    }

}
