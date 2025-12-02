using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ItemSlot : MonoBehaviour, IPointerClickHandler
{
    public ItemObject item;   //Item Object
    public int quantity;

    public Image icon;
    public TMP_Text quantityText;

    // Description section
    public Image itemDescriptionImage;
    public TMP_Text itemDescriptionNameText;
    public TMP_Text itemDescriptionDetailsText;

    public GameObject selectedRect;
    public bool thisItemSelected;

    private InventoryManager inventoryManager;

    private void Start()
    {
        inventoryManager = InventoryManager.Instance;
        UpdateSlotUI();
    }

    public int AddItem(ItemObject newItem, int amount)
    {
        // If empty slot Å® assign item
        if (item == null)
        {
            item = newItem;
            quantity = 0;
        }

        // If different item in slot Å® cannot store
        if (item != newItem)
            return amount;

        int spaceLeft = item.maxStack - quantity;
        int addAmount = Mathf.Min(spaceLeft, amount);

        quantity += addAmount;
        amount -= addAmount;

        UpdateSlotUI();
        return amount;
    }

    public void UpdateSlotUI()
    {
        if (item == null || quantity == 0)
        {
            icon.enabled = false;
            quantityText.enabled = false;
            return;
        }

        icon.enabled = true;
        icon.sprite = item.icon;

        quantityText.enabled = true;
        quantityText.text = quantity.ToString();
    }

    public void ClearSlot()
    {
        item = null;
        quantity = 0;
       
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
            OnLeftClick();
    }

    public void OnLeftClick()
    {
        inventoryManager.DeselectAllSlots();
        selectedRect.SetActive(true);
        thisItemSelected = true;

        if (item != null)
        {
            // show details
            itemDescriptionNameText.text = item.itemName;
            itemDescriptionDetailsText.text = item.description;
            itemDescriptionImage.sprite = item.icon;
        }
        else
        {
            // empty slot Å® clear description
            itemDescriptionNameText.text = "";
            itemDescriptionDetailsText.text = "";
            itemDescriptionImage.sprite = null;
        }
    }

}
