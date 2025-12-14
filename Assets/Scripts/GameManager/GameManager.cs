using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; set; }

    [Header("Game Slots")]
    public int slotCount = 3;

    [Header("Scenes (by name)")]
    public string mainMenuScene = "MainMenu";
    public string introScene = "GameIntro";
    public string gameScene = "";

    [HideInInspector] public int currentSlot = -1;
    [HideInInspector] public SaveData currentSave = null;

    public SceneFader faderPrefab;
    private SceneFader faderInstance;

    public GameObject pauseMenuPrefab;
    PauseMenuController pauseMenuInstance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            DontDestroyOnLoad(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        EnsureFaderExists();
    }

    void EnsureFaderExists()
{
    if (faderInstance == null)
    {
        faderInstance = FindFirstObjectByType<SceneFader>();

        if (faderInstance == null)
        {
            // Instantiate dynamically
            SceneFader prefab = faderPrefab;
            if (prefab != null)
            {
                faderInstance = Instantiate(prefab);
                DontDestroyOnLoad(faderInstance.gameObject);
            }
            else
            {
                Debug.LogError("Fader prefab not assigned on GameManager!");
            }
        }
    }
}

    public void StartNewGame(int slot)
    {
        currentSlot = slot;
        currentSave = SaveData.CreateDefault();
        currentSave.saveName = $"Slot {slot + 1} - New Game";
        SaveSystem.SaveSlot(slot, currentSave);
        StartCoroutine(DoLoadIntro());
    }

    public void LoadGame(int slot)
    {
        if (!SaveSystem.SlotExists(slot))
        {
            StartNewGame(slot);
            return;
        }

        currentSlot = slot;
        currentSave = SaveSystem.LoadSlot(slot);
        StartCoroutine(DoLoadGameplay());
    }

    public void FinishIntroAndStartGameplay()
    {
        StartCoroutine(DoLoadGameplay());
    }

    IEnumerator DoLoadIntro()
    {
        EnsureFaderExists();
        if (faderInstance != null) yield return faderInstance.FadeOutRoutine();

        AsyncOperation op = SceneManager.LoadSceneAsync(introScene);
        yield return op;

        EnsureFaderExists();
        if (faderInstance != null) yield return faderInstance.FadeInRoutine();
    }

    IEnumerator DoLoadGameplay()
    {
        EnsureFaderExists();
        if (faderInstance != null) yield return faderInstance.FadeOutRoutine();

        AsyncOperation op = SceneManager.LoadSceneAsync(gameScene);
        yield return op;

        EnsureFaderExists();
        if (faderInstance != null) yield return faderInstance.FadeInRoutine();

        PlayerLoader loader = FindFirstObjectByType<PlayerLoader>();
        if (loader != null && currentSave != null)
        {
            loader.ApplySave(currentSave);
        }
    }

    // Save the current state (call from gameplay when saving)
    public void SaveCurrent()
    {
        if (currentSlot < 0 || currentSave == null) return;
        SaveSystem.SaveSlot(currentSlot, currentSave);
    }

    public void LoadSceneFromEdge(string sceneName, string spawnPoint = null)
    {
        StartCoroutine(LoadSceneRoutine(sceneName, spawnPoint));
    }

    IEnumerator LoadSceneRoutine(string targetScene, string spawnPointName)
    {
        var fader = SceneFader.Instance;
        if (fader != null) yield return fader.FadeOutRoutine();

        // Start async loading
        AsyncOperation op = SceneManager.LoadSceneAsync(targetScene);
        yield return op;

        if (fader != null) yield return fader.FadeInRoutine();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string name = scene.name;
        // Compare with the gameplay scene name you already have in GameManager
        if (name.StartsWith("Scene")) // or use gameplayScene field name
        {
            SpawnPauseMenu();
        }
        else
        {
            DestroyPauseMenu(); // safe fallback
        }
    }

    void SpawnPauseMenu()
    {
        if (pauseMenuPrefab == null) return;
        if (pauseMenuInstance != null) return; // already exists

        // Instantiate under no parent so it's top-level UI. If you want it inside a Canvas, parent appropriately.
        GameObject go = Instantiate(pauseMenuPrefab);
        pauseMenuInstance = go.GetComponent<PauseMenuController>();

        // Optionally set name and DontDestroyOnLoad so it persists through subscene loads:
        go.name = "PauseMenu_Instance";
        DontDestroyOnLoad(go);

        // Ensure the pause menu is initially hidden (prefab already handles this).
    }

    void DestroyPauseMenu()
    {
        if (pauseMenuInstance == null) return;
        Destroy(pauseMenuInstance.gameObject);
        pauseMenuInstance = null;
    }
}
