using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuController : MonoBehaviour
{
    [Header("UI")]
    public GameObject pauseMenuUI;
    public CanvasGroup canvasGroup;
    public Button resumeButton;
    public Button settingsButton;
    public Button homeButton;

    bool isPaused = false;

    private PlayerControls inputActions;

    private void Awake()
    {
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);

        // Hook buttons
        if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
        if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
        if (homeButton != null) homeButton.onClick.AddListener(Home);

        inputActions = new PlayerControls();
        inputActions.Player.Pause.performed += OnPausePerformed;
    }

    private void OnEnable()
    {
        inputActions?.Player.Enable();
    }

    private void OnDisable()
    {
        inputActions?.Player.Disable();
    }

    private void OnDestroy()
    {
        if (inputActions != null) inputActions.Player.Pause.performed -= OnPausePerformed;
    }

    private void OnPausePerformed(InputAction.CallbackContext ctx)
    {
        if (isPaused) Resume();
        else Show();
    }

    private void Show()
    {
        Debug.Log("PauseMenuController Show");
        if (isPaused)
        {
            Debug.Log("Game already paused.");
            return;
        }
        Debug.Log("Pausing game...");
        isPaused = true;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        pauseMenuUI.SetActive(true);

        // Pause game time
        Time.timeScale = 0f;
    }

    public void Resume()
    {
        if (!isPaused) return;
        isPaused = false;

        // Unpause
        Time.timeScale = 1f;

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        pauseMenuUI.SetActive(false);
    }

    public void OpenSettings()
    {
        // Open settings menu
    }

    public void Home()
    {
        // Ensure timeScale restored
        Time.timeScale = 1f;

        GameManager.Instance.ResetGameplayState();
        if (GameManager.Instance != null) GameManager.Instance.LoadSceneFromEdge(GameManager.Instance.mainMenuScene);
        else SceneManager.LoadScene("MainMenu");
    }

}
