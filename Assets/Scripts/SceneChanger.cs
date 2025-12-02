using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneEdgeLoader : MonoBehaviour
{
    public string targetScene;       // The scene to load
    public string spawnPointName;    // Which spawn point to use in the next scene

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // Store which spawn point the next scene should use
            SpawnManager.lastSpawnPoint = spawnPointName;

            // Load the next scene
            SceneManager.LoadScene(targetScene);
        }
    }
}
