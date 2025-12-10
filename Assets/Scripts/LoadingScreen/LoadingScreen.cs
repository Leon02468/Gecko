using UnityEngine;

public class LoadingScreen : MonoBehaviour
{
    public static LoadingScreen Instance { get; private set; }
    public GameObject loadingPanel;

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

        if (loadingPanel != null)
            loadingPanel.SetActive(false);
    }

    public void Show()
    {
        if (loadingPanel != null)
            loadingPanel.SetActive(true);
    }

    public void Hide()
    {
        if (loadingPanel != null)
            loadingPanel.SetActive(false);
    }
}
