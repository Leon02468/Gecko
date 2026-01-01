using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; set; }

    [Header("Scenes (by name)")]
    public string mainMenuScene = "MainMenu";
    public string introScene = "GameIntro";

    [Header("Save")]
    public int slotCount = 3;
    [HideInInspector] public int currentSlot = -1;
    [HideInInspector] public SaveData currentSave = null;

    [Header("Prefabs")]
    public SceneFader faderPrefab;
    public GameObject eventSystemPrefab;
    public GameObject audioManagerPrefab;
    public GameObject playerPrefab;
    public GameObject pauseMenuPrefab;
    public GameObject inventoryPrefab;
    public GameObject hotBarPrefab;
    public GameObject itemDatabasePrefab;
    
    /*Runtime Instances*/
    SceneFader fader;
    GameObject eventSystem;
    AudioManager audioManager;
    GameObject player;
    PauseMenuController pauseMenu;
    InventoryManager inventory;
    GameObject hotBar;
    ItemDatabase itemDatabase;

    /*External Use*/
    public AudioManager AudioInstance => audioManager;
    public GameObject PlayerInstance => player;
    public InventoryManager InventoryInstance => inventory;
    public ItemDatabase ItemDatabaseInstance => itemDatabase;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        EnsureFader();
        EnsureAudio();
        EnsureEventSystem();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }


    /*GAME MANAGER LOGIC*/
    // Main Menu Logic
    public void StartNewGame(int slot)
    {
        currentSlot = slot;
        currentSave = SaveData.CreateDefault();
        currentSave.saveName = $"Slot {slot + 1}";
        SaveSystem.SaveSlot(slot, currentSave);

        StartCoroutine(LoadIntroRoutine());
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
        StartCoroutine(LoadGameplayRoutine());
        inventory.ApplyInventorySnapshot(currentSave.inventory);
        MoneyManager.Instance.ApplyMoneySnapshot(currentSave.money);
    }

    public void FinishIntroAndStartGameplay()
    {
        StartCoroutine(LoadGameplayRoutine());
    }

    IEnumerator LoadIntroRoutine()
    {
        yield return FadeOut();

        AsyncOperation op = SceneManager.LoadSceneAsync(introScene);
        yield return op;

        yield return FadeIn();
    }

    IEnumerator LoadGameplayRoutine()
    {
        yield return FadeOut();

        int sceneIndex = currentSave.sceneBuildIndex;
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneIndex);
        yield return op;

        SpawnPlayer();
        SpawnInventory();
        SpawnPauseMenu();
        SpawnHotBar();
        SpawnItemDatabase();

        PlayerLoader loader = FindFirstObjectByType<PlayerLoader>();
        if (loader != null && currentSave != null)
        {
            loader.ApplySave(currentSave);
        }

        yield return FadeIn();
    }

    // Edge Move Logic
    bool pendingEdgeMove = false;
    string pendingSpawnPoint = null;

    public void PrepareEdgeMove(string spawnPointName)
    {
        pendingEdgeMove = true;
        pendingSpawnPoint = spawnPointName;
    }

    public void LoadSceneFromEdge(string sceneName, string spawnPoint = null)
    {
        StartCoroutine(LoadSceneRoutine(sceneName, spawnPoint));
    }

    IEnumerator LoadSceneRoutine(string targetScene, string spawnPointName)
    {
        yield return FadeOut();
        
        AsyncOperation op = SceneManager.LoadSceneAsync(targetScene);
        yield return op;
        ApplyEdgeMoveIfNeeded();
        if (player != null)
            BindCameraToPlayer(player);

        yield return FadeIn();
    }

    void ApplyEdgeMoveIfNeeded()
    {
        if (!pendingEdgeMove || player == null) return;

        GameObject sp = GameObject.Find(pendingSpawnPoint);
        if (sp != null)
        {
            var rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.simulated = false;
            }

            player.transform.position = sp.transform.position;

            if (rb != null)
                rb.simulated = true;
        }

        pendingEdgeMove = false;
        pendingSpawnPoint = null;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == mainMenuScene)
        {
            DestroyGameplayInstances();
        }
    }

    //Camera Logic
    void BindCameraToPlayer(GameObject playerInstance)
    {
        Transform camTarget = playerInstance.transform.Find("CameraTarget");

        var cams = FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None);
        if (cams == null)
        {
            Debug.LogWarning("No CinemachineVirtualCamera found.");
            return;
        }

        foreach (var cam in cams)
        {
            cam.Follow = camTarget;
            cam.LookAt = player.transform;
        }
    }


    /*SPAWN & DESTROY INSTANCES*/
    // Spawn & Ensure Methods
    void EnsureFader()
    {
        if (fader != null) return;
        fader = FindFirstObjectByType<SceneFader>();

        if (fader == null && faderPrefab != null)
        {
            fader = Instantiate(faderPrefab);
            DontDestroyOnLoad(fader.gameObject);
        }
    }

    void EnsureAudio()
    {
        if (audioManagerPrefab == null) return;
        if (audioManager != null) return;

        GameObject go = Instantiate(audioManagerPrefab);
        audioManager = go.GetComponent<AudioManager>();

        go.name = "AudioManager_Instance";
        DontDestroyOnLoad(go);
    }

    void EnsureEventSystem()
    {
        if (eventSystemPrefab == null || eventSystem != null) return;

        eventSystem = Instantiate(eventSystemPrefab);
        DontDestroyOnLoad(eventSystem);
    }

    void SpawnPlayer()
    {
        if (player != null) Destroy(player);

        Vector3 spawnPos = new Vector3(
            currentSave.playerX,
            currentSave.playerY,
            0
        );

        player = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
        DontDestroyOnLoad(player);

        player.GetComponent<PlayerLoader>()?.ApplySave(currentSave);

        BindCameraToPlayer(player);
    }

    void SpawnPauseMenu()
    {
        if (pauseMenuPrefab == null || pauseMenu != null) return;

        GameObject go = Instantiate(pauseMenuPrefab);
        pauseMenu = go.GetComponent<PauseMenuController>();

        go.name = "PauseMenu_Instance";
        DontDestroyOnLoad(go);
    }

    void SpawnInventory()
    {
        if (inventoryPrefab == null || inventory != null) return;

        GameObject go = Instantiate(inventoryPrefab);
        inventory = go.GetComponent<InventoryManager>();

        go.name = "Inventory_Instance";
        DontDestroyOnLoad(go);
    }
    void SpawnHotBar()
    {
        if (hotBarPrefab == null || hotBar != null) return;
        hotBar = Instantiate(hotBarPrefab);

        hotBar.name = "HotBar_Instance";
        DontDestroyOnLoad(hotBar);
    }

    void SpawnItemDatabase()
    {
        if (itemDatabasePrefab == null || itemDatabase != null) return;
        GameObject go = Instantiate(itemDatabasePrefab);
        itemDatabase = go.GetComponent<ItemDatabase>();

        go.name = "ItemDatabase_Instance";
        DontDestroyOnLoad(go);
    }

    // Destroy Method
    void DestroyGameplayInstances()
    {
        if (player != null) Destroy(player);
        if (pauseMenu != null) Destroy(pauseMenu.gameObject);
        if (inventory != null) Destroy(inventory.gameObject);
        if (hotBar != null) Destroy(hotBar.gameObject);
        if (itemDatabase != null) Destroy(itemDatabase.gameObject);

        player = null;
        pauseMenu = null;
        inventory = null;
        hotBar = null;
        itemDatabase = null;
    }


    /*SAVE SYSTEM*/
    public void SaveGameEvent(string reason = "")
    {
        if (currentSlot < 0 || currentSave == null) return;

        // Update save data
        currentSave.sceneBuildIndex = SceneManager.GetActiveScene().buildIndex;
        if (player != null)
        {
            var health = player.GetComponent<PlayerHealth>();
            if (health != null)
                currentSave.playerHealth = health.CurrentHP;
        }
        currentSave.savedAtTicks = System.DateTime.UtcNow.Ticks;
        currentSave.inventory = inventory.GetInventorySnapshot();
        currentSave.money = MoneyManager.Instance.GetMoneySnapshot();

        SaveSystem.SaveSlot(currentSlot, currentSave);
        Debug.Log($"[SAVE] {reason}");
    }


    /*HELPER*/
    IEnumerator FadeOut()
    {
        EnsureFader();
        if (fader != null)
            yield return fader.FadeOutRoutine();
    }

    IEnumerator FadeIn()
    {
        EnsureFader();
        if (fader != null)
            yield return fader.FadeInRoutine();
    }
}
