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

    private void Awake()
    {
        inputActions = new PlayerControls();
        inputActions.Player.Inventory.performed += ToggleInventory;
        inputActions.Inventory.CloseInventory.performed += ToggleInventory;
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

    public void AddItem(string itemName, int quantity, Sprite itemSprite)
    {
        for (int i = 0; i < itemSlot.Length; i++)
        {
            if (itemSlot[i].isFull == false)
            {
                itemSlot[i].AddItem(itemName, quantity, itemSprite);
                return;
            }
        }
    }
}
