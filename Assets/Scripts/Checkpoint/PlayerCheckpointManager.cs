using UnityEngine;

public class PlayerCheckpointManager : MonoBehaviour
{
    public static PlayerCheckpointManager Instance { get; private set; }
    private Vector3 checkpointPosition;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetCheckpoint(Vector3 position)
    {
        checkpointPosition = position;
    }

    public Vector3 GetCheckpoint()
    {
        return checkpointPosition;
    }
}
