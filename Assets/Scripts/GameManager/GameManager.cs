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
            Debug.LogWarning($"Slot {slot} doesn't exist. Starting new instead.");
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
}
