using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveCheckpoint : MonoBehaviour, IInteractable
{
    public Transform t;

    public bool CanInteract() => true;

    public void Interact()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.currentSave == null) return;

        Vector3 pos = t.position;

        gm.currentSave.playerX = pos.x;
        gm.currentSave.playerY = pos.y;
        gm.currentSave.sceneBuildIndex = SceneManager.GetActiveScene().buildIndex;
        gm.currentSave.savedAtTicks = System.DateTime.UtcNow.Ticks;

        gm.SaveCurrent();
        Debug.Log("Game saved at checkpoint");
    }
}
