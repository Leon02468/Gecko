using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoadFinisher : MonoBehaviour
{
    void Start()
    {
        if (LoadingScreen.Instance != null)
        {
            LoadingScreen.Instance.Hide();
        }
    }
}
