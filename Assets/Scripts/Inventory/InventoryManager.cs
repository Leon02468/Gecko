using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.IO;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;


    [Header("UI / Slots")]
    public GameObject inventoryPanel;
    public ItemSlot[] itemSlot;

    [Header("Navigation")]
    private PlayerControls inputActions;
    private PlayerInput playerInput;
    private int selectedIndex = 0;
    [SerializeField] private int columns = 5;

    // Save path (visible in logs)
    private string savePath => Path.Combine(Application.persistentDataPath, "inventory.json");

    // ========== Unity lifecycle ==========
    private void Awake()
    {       

        // singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Input actions (keep your existing bindings)
        inputActions = new PlayerControls();
        inputActions.Player.Inventory.performed += ToggleInventory;
        inputActions.Inventory.CloseInventory.performed += ToggleInventory;
        inputActions.Inventory.NavigateLeft.performed += ctx => SelectPreviousSlot();
        inputActions.Inventory.NavigateRight.performed += ctx => SelectNextSlot();
        inputActions.Inventory.NavigateUp.performed += ctx => SelectSlotAbove();
        inputActions.Inventory.NavigateDown.performed += ctx => SelectSlotBelow();

        playerInput = FindAnyObjectByType<PlayerInput>();

        // Hook scene unload to save (e.g. when switching scenes)
        SceneManager.sceneUnloaded += OnSceneUnloaded;

#if UNITY_EDITOR
        // In Editor, Play Mode stop does not call OnApplicationQuit reliably.
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
#endif
    }

    private void Start()
    {
        // Load in Start so ItemDatabase Awake() has run already
        LoadInventory();
        if (inventoryPanel != null) inventoryPanel.SetActive(false);

        Debug.Log($"InventoryManager initialized. Save path: {savePath}");

    }

    private void OnEnable()
    {
        if (inputActions != null) inputActions.Player.Enable();
    }

    private void OnDisable()
    {
        if (inputActions != null) inputActions.Player.Disable();
    }

    private void OnDestroy()
    {
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
#if UNITY_EDITOR
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
#endif
    }

    // ========== Inventory UI toggle ==========
    private void ToggleInventory(InputAction.CallbackContext context)
    {
        if (inventoryPanel == null) return;

        bool isActive = inventoryPanel.activeSelf;
        inventoryPanel.SetActive(!isActive);

        if (!isActive)
        {
            

            Time.timeScale = 0f;
            if (playerInput != null) playerInput.enabled = false;

            inputActions.Player.Disable();
            inputActions.Inventory.Enable();
            
            SelectSlot(selectedIndex);
        }
        else
        {
            Time.timeScale = 1f;
            if (playerInput != null) playerInput.enabled = true;

            inputActions.Player.Enable();
            inputActions.Inventory.Disable();

            // Optional: save when closing inventory (helps ensure persistence)
            SaveInventory();
        }
    }

    // ========== Add item (stacks and fills) ==========
    public int AddItem(ItemObject itemObject, int quantity)
    {
        if (itemObject == null || quantity <= 0) return quantity;

        int original = quantity;
        int leftOver = quantity;

        // Stack into existing slots
        foreach (var slot in itemSlot)
        {
            if (slot == null) continue; // Prevent NullReferenceException
            if (slot.item == itemObject)
            {
                leftOver = slot.AddItem(itemObject, leftOver);
                if (leftOver <= 0)
                {
                    SaveInventoryIfChanged(original, leftOver);
                    return 0;
                }
            }
        }

        // Fill empty slots
        foreach (var slot in itemSlot)
        {
            if (slot == null) continue; // Prevent NullReferenceException
            if (slot.item == null)
            {
                leftOver = slot.AddItem(itemObject, leftOver);
                if (leftOver <= 0)
                {
                    SaveInventoryIfChanged(original, leftOver);
                    return 0;
                }
            }
        }

        // If we reach here, inventory couldn't accept everything
        SaveInventoryIfChanged(original, leftOver);
        return leftOver;
    }

    // Helper: save only if something actually changed
    private void SaveInventoryIfChanged(int originalQuantity, int leftOver)
    {
        if (leftOver != originalQuantity)
        {
            SaveInventory();
        }
    }

    // ========== Slot selection (unchanged logic) ==========
    public void DeselectAllSlots()
    {
        foreach (var slot in itemSlot)
        {
            if (slot == null) continue;
            if (slot.selectedRect != null)
                slot.selectedRect.SetActive(false);
            slot.thisItemSelected = false;
        }
    }

    public void SelectSlot(int index)
    {
        DeselectAllSlots();
        selectedIndex = Mathf.Clamp(index, 0, itemSlot.Length - 1);
        itemSlot[selectedIndex].selectedRect.SetActive(true);
        itemSlot[selectedIndex].thisItemSelected = true;

        // Update description for keyboard navigation
        itemSlot[selectedIndex].OnLeftClick();
    }


    public void SelectNextSlot() => SelectSlot((selectedIndex + 1) % itemSlot.Length);
    public void SelectPreviousSlot() => SelectSlot((selectedIndex - 1 + itemSlot.Length) % itemSlot.Length);

    public void SelectSlotAbove()
    {
        int above = selectedIndex - columns;
        if (above < 0) above += itemSlot.Length;
        SelectSlot(above);
    }

    public void SelectSlotBelow()
    {
        int below = selectedIndex + columns;
        if (below >= itemSlot.Length) below -= itemSlot.Length;
        SelectSlot(below);
    }

    // ========== SAVE / LOAD ==========
    // Save inventory into JSON (itemID + quantity per slot)
    public void SaveInventory()
    {
        try
        {
            List<ItemSlotSave> saveList = new List<ItemSlotSave>();

            foreach (var slot in itemSlot)
            {
                if (slot == null)
                    saveList.Add(new ItemSlotSave { itemID = -1, quantity = 0 });
                else
                    saveList.Add(new ItemSlotSave
                    {
                        itemID = slot.item != null ? slot.item.itemID : -1,
                        quantity = slot.quantity
                    });
            }

            string json = JsonUtility.ToJson(new SaveWrapper(saveList), true);
            File.WriteAllText(savePath, json);
            Debug.Log($"Saved inventory ({saveList.Count} slots) to: {savePath}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Failed to save inventory: " + ex);
        }
    }

    // Load inventory from JSON; resilient to missing database entries
    public void LoadInventory()
    {
        try
        {
            if (!File.Exists(savePath))
            {
                Debug.Log("No inventory save file found at: " + savePath);
                return;
            }

            string json = File.ReadAllText(savePath);
            var wrapper = JsonUtility.FromJson<SaveWrapper>(json);

            if (wrapper == null || wrapper.items == null)
            {
                Debug.LogWarning("Save file malformed or empty.");
                return;
            }

            for (int i = 0; i < itemSlot.Length; i++)
            {
                if (itemSlot[i] == null) continue;
                // clear slot first
                itemSlot[i].ClearSlot();

                if (i < wrapper.items.Count)
                {
                    var data = wrapper.items[i];
                    if (data.itemID != -1)
                    {
                        // Use ItemDatabase to map id -> ItemObject
                        if (ItemDatabase.Instance == null)
                        {
                            Debug.LogError("ItemDatabase instance not present in scene. Cannot load items by ID.");
                            continue;
                        }

                        ItemObject loadedItem = ItemDatabase.GetItemByID(data.itemID);
                        if (loadedItem == null)
                        {
                            Debug.LogWarning($"Loaded itemID {data.itemID} not found in ItemDatabase. Skipping slot {i}.");
                            continue;
                        }
                        itemSlot[i].item = loadedItem;
                        itemSlot[i].quantity = data.quantity;
                        itemSlot[i].UpdateSlotUI();
                    }
                }
            }

            Debug.Log("Inventory loaded from " + savePath);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Failed to load inventory: " + ex);
        }
    }

    // Save on application quit (builds)
    private void OnApplicationQuit()
    {
        SaveInventory();
    }

    // Save on scene unload (scene switching)
    private void OnSceneUnloaded(Scene current)
    {
        SaveInventory();
    }

#if UNITY_EDITOR
    // Save on exiting play mode in editor
    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            SaveInventory();
            Debug.Log("Saved inventory because editor play mode is exiting.");
        }
    }
#endif

    // small helpers + data structures
    [System.Serializable]
    public class ItemSlotSave
    {
        public int itemID;
        public int quantity;
    }

    [System.Serializable]
    public class SaveWrapper
    {
        public List<ItemSlotSave> items;
        public SaveWrapper(List<ItemSlotSave> items) { this.items = items; }
    }
}
