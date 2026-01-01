using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneEdgeLoader : MonoBehaviour
{
    public string targetScene;       // The scene to load
    public string spawnPointName;    // Which spawn point to use in the next scene

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        // request spawn point
        SpawnManager.lastSpawnPoint = spawnPointName;

        GameManager.Instance.PrepareEdgeMove(spawnPointName);
        GameManager.Instance.LoadSceneFromEdge(targetScene);
    }
}
