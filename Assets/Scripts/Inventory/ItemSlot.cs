using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ItemSlot : MonoBehaviour, IPointerClickHandler
{
    //===ITEM DATA===//
    public string itemName;
    public int quantity;
    public Sprite itemSprite;
    public bool isFull;
    public string itemDescription;

    public int maxNumberOfItems;

    //===ITEM SLOT===//
   
    public TMP_Text quantityText;

 
    public Image itemImage;


    //===ITEM DESCRIPTION SLOT===//
    public Image itemDescriptionImage;
    public TMP_Text itemDescriptionNameText;
    public TMP_Text itemDescriptionDetailsText;


    public GameObject selectedRect;
    public bool thisItemSelected;

    private InventoryManager inventoryManager; // Enable this script to talk to InventoryManager

    private void Start()
    {
        inventoryManager = GameObject.Find("InventoryCanvas").GetComponent<InventoryManager>();
    }

    public int AddItem(string itemName, int quantity, Sprite itemSprite, string itemDescription)
    {
        // If this slot is full, skip
        if (isFull)
        {
            Debug.Log($"Slot {gameObject.name} is full, skipping.");
            return quantity;
        }

        // Update name and description
        this.itemName = itemName;
        this.itemDescription = itemDescription;
        this.itemSprite = itemSprite;

        // Update visuals
        itemImage.sprite = itemSprite;
        itemImage.enabled = true;

        // Add quantity
        this.quantity += quantity;

        // Cap at max
        int extraItems = 0;
        if (this.quantity >= maxNumberOfItems)
        {
            extraItems = this.quantity - maxNumberOfItems;
            this.quantity = maxNumberOfItems;
            isFull = true;
        }

        // Update text
        quantityText.text = this.quantity.ToString();
        quantityText.enabled = true;

        Debug.Log($"[{gameObject.name}] Added {quantity}, now has {this.quantity}, leftover = {extraItems}");

        return extraItems;

    }


    public void OnPointerClick(PointerEventData eventData)
    {
        if(eventData.button == PointerEventData.InputButton.Left)
        {
            OnLeftClick();
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            OnRightClick();
        }
    }

    public void OnLeftClick()
    {
        inventoryManager.DeselectAllSlots();
        selectedRect.SetActive(true);
        thisItemSelected = true;
        itemDescriptionNameText.text = itemName;
        itemDescriptionDetailsText.text = itemDescription;
        itemDescriptionImage.sprite = itemSprite;
    }

    public void OnRightClick()
    {
       
    }
}
