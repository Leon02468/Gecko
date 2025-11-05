using UnityEngine;
using UnityEngine.InputSystem;
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

    [SerializeField]
    private int columns = 5;

    private void Awake()
    {
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

    void Start()
    {
        inventoryPanel.SetActive(false);
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
    
}
