using UnityEngine;

public class CameraChoosing : MonoBehaviour
{
    public bool chosen = false;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;
        chosen = true;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;
        chosen = false;
    }
}
