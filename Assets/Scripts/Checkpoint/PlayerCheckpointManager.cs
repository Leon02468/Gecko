using UnityEngine;

public class PlayerCheckpointManager : MonoBehaviour
{
    private Vector3 checkpointPosition;
    public void SetCheckpoint(Vector3 position)
    {
        checkpointPosition = position;
    }

    public Vector3 GetCheckpoint()
    {
        return checkpointPosition;
    }
}
