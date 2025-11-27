using UnityEngine;
using UnityEngine.InputSystem;
using System.IO;
using System.Collections.Generic;
public class InventoryManager : MonoBehaviour
{
    // Reference to the inventory panel UI
    public GameObject inventoryPanel;
    // Input action asset
    private PlayerControls inputActions;
    // Add reference to the PlayerInput component
    private PlayerInput playerInput;
    // Reference to ItemSlot
    public ItemSlot[] itemSlot;
    //Add a field to track the currently selected slot
    private int selectedIndex = 0;

    //Instance
    public static InventoryManager Instance;

    [SerializeField]
    private int columns = 5;

    private void Awake()
    {
        // Singleton so only one inventory exists
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);  // <--- THIS IS THE IMPORTANT LINE


        inputActions = new PlayerControls();
        inputActions.Player.Inventory.performed += ToggleInventory;
        inputActions.Inventory.CloseInventory.performed += ToggleInventory;
        inputActions.Inventory.NavigateLeft.performed += ctx => SelectPreviousSlot();
        inputActions.Inventory.NavigateRight.performed += ctx => SelectNextSlot();
        inputActions.Inventory.NavigateUp.performed += ctx => SelectSlotAbove();
        inputActions.Inventory.NavigateDown.performed += ctx => SelectSlotBelow();
        playerInput = FindAnyObjectByType<PlayerInput>();

        Debug.Log("InventoryManager: PlayerControls created");
    }
    private void OnEnable()
    {
        inputActions.Player.Enable();
    }

    private void OnDisable()
    {
        inputActions.Player.Disable();
    }

    private void ToggleInventory(InputAction.CallbackContext context)
    {
        bool isActive = inventoryPanel.activeSelf;
        inventoryPanel.SetActive(!isActive);
        if (!isActive)
        {
            Time.timeScale = 0f; // Pause the game

            // Disable the PlayerInput component to block all gameplay input
            if (playerInput != null)
                playerInput.enabled = false;

            // Disable input action maps
            inputActions.Player.Disable();
            inputActions.Inventory.Enable();

            //Select first slot when opening inventory
            SelectSlot(selectedIndex);

            Debug.Log($"PlayerInput disabled: {!playerInput.enabled}");
        }
        else
        {
            Time.timeScale = 1f; // Resume the game

            // Re-enable the PlayerInput component
            if (playerInput != null)
                playerInput.enabled = true;

            // Re-enable input action maps
            inputActions.Player.Enable();
            inputActions.Inventory.Disable();

            Debug.Log($"PlayerInput enabled: {playerInput.enabled}");
        }
    }

    public int AddItem(string itemName, int quantity, Sprite itemSprite, string itemDescription)
    {
        int leftOverItems = quantity;
        Debug.Log($"Trying to add {quantity}x {itemName}...");

        //Filling the existing stack first
        for (int i = 0; i < itemSlot.Length; i++)
        {
            if (itemSlot[i].quantity == 0 || (itemSlot[i].isFull == false && itemSlot[i].itemName == itemName))
            {
                leftOverItems = itemSlot[i].AddItem(itemName, leftOverItems, itemSprite, itemDescription);
                
                if (leftOverItems <= 0)
                {
                    return 0;
                }
            }
            Debug.Log($"Slot {i}: now has {itemSlot[i].quantity}, leftover = {leftOverItems}");

        }


        for (int i = 0; i < itemSlot.Length; i++)
        {
            Debug.Log($"[Slot {i}] name={itemSlot[i].itemName}, qty={itemSlot[i].quantity}, full={itemSlot[i].isFull}");
        }


        return leftOverItems; // return left over items if inventory is full
    }

    public void DeselectAllSlots()
    {
        for (int i = 0; i < itemSlot.Length; i++) 
        {
            itemSlot[i].selectedRect.SetActive(false);
            itemSlot[i].thisItemSelected = false;
        }
    }

    public void SelectSlot(int index)
    {
        DeselectAllSlots();
        selectedIndex = Mathf.Clamp(index, 0, itemSlot.Length - 1);
        itemSlot[selectedIndex].selectedRect.SetActive(true);
        itemSlot[selectedIndex].thisItemSelected = true;
    }

    public void SelectNextSlot()
    {
        int next = (selectedIndex + 1) % itemSlot.Length;
        SelectSlot(next);
    }

    public void SelectPreviousSlot()
    {
        int previous = (selectedIndex - 1 + itemSlot.Length) % itemSlot.Length;
        SelectSlot(previous);
    }

    public void SelectSlotAbove()
    {
        int above = selectedIndex - columns;
        if (above < 0 )
        {
            // Calculate the last row index for the current column
            int lastRow = (itemSlot.Length - 1) / columns;
            above = lastRow * columns + (selectedIndex % columns);
            if (above >= itemSlot.Length)
            {
                above -= columns; // Adjust if it goes out of bounds
            }
        }
        SelectSlot(above);
    }

    public void SelectSlotBelow()
    {
        int below = selectedIndex + columns;
        if (below >= itemSlot.Length)
        {
            below = selectedIndex % columns; 
        }
        SelectSlot(below);
    }

    private string savePath => Path.Combine(Application.persistentDataPath, "inventory.json");

    public void SaveInventory()
    {
        List<ItemSlotData> slotDataList = new List<ItemSlotData>();
        foreach (var slot in itemSlot)
        {
            var data = new ItemSlotData
            {
                itemName = slot.itemName,
                quantity = slot.quantity,
                itemSpriteName = slot.itemSprite != null ? slot.itemSprite.name : "",
                isFull = slot.isFull,
                itemDescription = slot.itemDescription
            };
            slotDataList.Add(data);
        }
        string json = JsonUtility.ToJson(new SerializationWrapper<ItemSlotData>(slotDataList));
        File.WriteAllText(savePath, json);
        Debug.Log("Inventory saved to " + savePath);
    }

    public void LoadInventory()
    {
        if (!File.Exists(savePath)) return;

        string json = File.ReadAllText(savePath);
        Debug.Log($"Loading inventory JSON: {json}"); // Add this line

        var wrapper = JsonUtility.FromJson<SerializationWrapper<ItemSlotData>>(json);
        for (int i = 0; i < itemSlot.Length && i < wrapper.items.Count; i++)
        {
            var data = wrapper.items[i];
            Debug.Log($"Loading slot {i}: name={data.itemName}, qty={data.quantity}, spriteName={data.itemSpriteName}"); // Add this line

            itemSlot[i].itemName = data.itemName;
            itemSlot[i].quantity = data.quantity;
            itemSlot[i].itemSprite = LoadSpriteByName(data.itemSpriteName);
            itemSlot[i].isFull = data.isFull;
            itemSlot[i].itemDescription = data.itemDescription;

            // Update visuals
            itemSlot[i].itemImage.sprite = itemSlot[i].itemSprite;
            itemSlot[i].itemImage.enabled = itemSlot[i].itemSprite != null;
            itemSlot[i].quantityText.text = itemSlot[i].quantity.ToString();
            itemSlot[i].quantityText.enabled = itemSlot[i].quantity > 0;
        }
        Debug.Log("Inventory loaded from " + savePath);
    }

    private Sprite LoadSpriteByName(string spriteName)
    {

        if (string.IsNullOrEmpty(spriteName))
        {
            Debug.Log("LoadSpriteByName: spriteName is null or empty");
            return null;
        }

        Debug.Log($"Trying to load sprite: {spriteName}");

        // Debug: List all available sprites in the folder
        Sprite[] allSprites = Resources.LoadAll<Sprite>("Sprites/Items");
        Debug.Log($"Found {allSprites.Length} sprites in Sprites/Items:");
        foreach (var sprite in allSprites)
        {
            Debug.Log($"  Available sprite: '{sprite.name}'");
        }

        // Try to load the specific sprite
        string resourcePath = "Sprites/Items/" + spriteName;
        Sprite loadedSprite = Resources.Load<Sprite>(resourcePath);

        if (loadedSprite == null)
        {
            Debug.LogError($"Failed to load sprite: {resourcePath}");
            Debug.LogError($"Looking for sprite named: '{spriteName}'");
            Debug.LogError($"Make sure the sprite exists at: Assets/Resources/{resourcePath}");

            // Try loading without the path to see if it exists at all
            Sprite testSprite = Resources.Load<Sprite>(spriteName);
            if (testSprite != null)
            {
                Debug.Log($"Found sprite '{spriteName}' at root level, but expected it in Sprites/Items/");
            }
        }
        else
        {
            Debug.Log($"Successfully loaded sprite: {spriteName}");
        }

        return loadedSprite;
    }

    [System.Serializable]
    private class SerializationWrapper<T>
    {
        public List<T> items;
        public SerializationWrapper(List<T> items) { this.items = items; }
    }

    // Call SaveInventory() when quitting, and LoadInventory() on Awake/Start
    private void OnApplicationQuit()
    {
        SaveInventory();
    }

    private void Start()
    {
        Sprite testSprite = Resources.Load<Sprite>("Sprites/Items/Cherries_0");
        if (testSprite != null)
        {
            Debug.Log($"SUCCESS: Found sprite directly: {testSprite.name}");
        }
        else
        {
            Debug.LogError("FAILED: Could not load sprite directly");
        }

        LoadInventory();
        inventoryPanel.SetActive(false);
    }


}
