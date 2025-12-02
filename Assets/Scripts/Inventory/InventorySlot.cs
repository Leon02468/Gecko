//using UnityEngine;
//using TMPro;
//using UnityEngine.UI;
//using UnityEngine.EventSystems;

//public class InventorySlot : MonoBehaviour, IPointerClickHandler
//{
//    public ItemObject item;
//    public int quantity;

//    public TMP_Text quantityText;
//    public Image itemImage;

//    public Image descImage;
//    public TMP_Text descName;
//    public TMP_Text descDetails;

//    public GameObject selectedHighlight;
//    public bool isSelected;

//    public int AddItem(ItemObject newItem, int amount)
//    {
//        // If slot empty, assign new item
//        if (item == null)
//        {
//            item = newItem;
//            quantity = 0;
//            UpdateVisuals();
//        }

//        // Check correct item type
//        if (item != newItem)
//            return amount;

//        int spaceLeft = item.maxStack - quantity;
//        int itemsToAdd = Mathf.Min(spaceLeft, amount);
//        quantity += itemsToAdd;
//        amount -= itemsToAdd;

//        UpdateVisuals();
//        return amount; // leftover
//    }

//    private void UpdateVisuals()
//    {
//        if (item != null)
//        {
//            itemImage.enabled = true;
//            itemImage.sprite = item.icon;
//            quantityText.text = quantity.ToString();
//            quantityText.enabled = true;
//        }
//        else
//        {
//            itemImage.enabled = false;
//            quantityText.enabled = false;
//        }
//    }

//    public void OnPointerClick(PointerEventData eventData)
//    {
//        if (eventData.button == PointerEventData.InputButton.Left)
//        {
//            SelectSlot();
//        }
//    }

//    public void SelectSlot()
//    {
//        InventoryManager.Instance.DeselectAllSlots();
//        selectedHighlight.SetActive(true);
//        isSelected = true;

//        if (item != null)
//        {
//            descName.text = item.itemName;
//            descDetails.text = item.description;
//            descImage.sprite = item.icon;
//        }
//    }

//    public void ClearSlot()
//    {
//        item = null;
//        quantity = 0;
//        UpdateVisuals();
//    }
//}
