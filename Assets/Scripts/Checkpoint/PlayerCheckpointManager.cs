using UnityEngine;

public class PlayerCheckpointManager : MonoBehaviour
{
    public static PlayerCheckpointManager Instance { get; private set; }

    private Vector3 checkpointPosition;
    private Vector3 savepointPosition;
    private bool hasSavepoint = false;

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

    // Called by checkpoint triggers
    public void SetCheckpoint(Vector3 position)
    {
        checkpointPosition = position;
    }

    // Called by savepoint triggers
   

    public Vector3 GetCheckpoint()
    {
        return checkpointPosition;
    }

   

    
}
