using System.Collections;
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
            StartCoroutine(LoadSceneAsync());
        }
    }

    IEnumerator LoadSceneAsync()
    {
        // Show loading panel
        if (LoadingScreen.Instance != null)
            LoadingScreen.Instance.Show();

        // Wait 0.1s so UI can update
        yield return new WaitForSeconds(0.1f);

        // Start async loading
        AsyncOperation op = SceneManager.LoadSceneAsync(targetScene);
        op.allowSceneActivation = false;

        // While loading...
        while (!op.isDone)
        {
            // When load reaches 90% ¨ scene ready
            if (op.progress >= 0.9f)
            {
                // Allow scene to activate
                op.allowSceneActivation = true;
            }

            yield return null;
        }
    }
}
