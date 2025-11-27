using UnityEngine;

public class SpawnPointSetter : MonoBehaviour
{
    private void Start()
    {
        Transform spawn = GameObject.Find(SpawnManager.lastSpawnPoint)?.transform;

        if (spawn != null)
        {
            transform.position = spawn.position;
        }
    }
}
