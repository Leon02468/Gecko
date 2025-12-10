using Unity.Cinemachine;
using UnityEngine;

public class CameraSwitcher : MonoBehaviour
{
    public CinemachineCamera bossRoomCam;
    public CameraChoosing leftCam;
    public CameraChoosing rightCam;

    private int higherPriority = 20;
    private int lowerPriority = 0;

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (leftCam.chosen)
            bossRoomCam.Priority = lowerPriority;
        else if (rightCam.chosen)
            bossRoomCam.Priority = higherPriority;
    }
}
