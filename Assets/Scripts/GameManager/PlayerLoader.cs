using UnityEngine;

public class PlayerLoader : MonoBehaviour
{
    public Transform playerTransform;
    public PlayerHealth playerHealthComponent;

    public void ApplySave(SaveData save)
    {
        if (playerTransform != null)
        {
            playerTransform.position = new Vector3(save.playerX, save.playerY, 0f);
        }

        if (playerHealthComponent != null)
        {
            playerHealthComponent.SetHealth(save.playerHealth);
        }

        // any other apply steps...
    }
}
