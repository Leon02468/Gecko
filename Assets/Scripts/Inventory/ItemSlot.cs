using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ItemSlot : MonoBehaviour,
    IPointerClickHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler,
    IDropHandler
{
    public ItemObject item;
    public int quantity;

    public Image icon;
    public TMP_Text quantityText;

    public Image itemDescriptionImage;
    public TMP_Text itemDescriptionNameText;
    public TMP_Text itemDescriptionDetailsText;

    public GameObject selectedRect;
    public bool thisItemSelected;

    private InventoryManager inventoryManager;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Canvas canvas;

    public static ItemSlot draggedSlot;
    public static Image dragIcon;


    private static int splitDragAmount = 0;// Amount being dragged (for split)
    private static ItemObject splitDragItem = null;// Item being split-dragged
    private static ItemSlot splitOriginSlot = null;



    private void Start()
    {
        inventoryManager = InventoryManager.Instance;
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        canvas = GetComponentInParent<Canvas>();
        UpdateSlotUI();
    }

    //private void Update()
    //{
    //    Debug.Log("ItemSlot Update running: " + gameObject.name);
    //}

    // ============================
    // DRAG START
    // ============================
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (item == null) return;

        // --- SPLIT STACK LOGIC FOR RIGHT MOUSE BUTTON ---

        if (eventData.button == PointerEventData.InputButton.Right && quantity > 1)
        {
            // Split half the stack for dragging
            splitDragAmount = quantity / 2;
            splitDragItem = item;
            splitOriginSlot = this;

            quantity -= splitDragAmount;
            UpdateSlotUI();

            // Set draggedSlot to null so OnDrop knows it's a split-drag
            draggedSlot = null;
        }
        else
        {
            // Default drag behavior (left mouse button)
            draggedSlot = this;
            splitDragAmount = 0;
            splitDragItem = null;
        }

        // Create the drag icon
        if (dragIcon == null)
        {
            dragIcon = new GameObject("Drag Icon").AddComponent<Image>();
            dragIcon.transform.SetParent(canvas.transform, false);
            dragIcon.raycastTarget = false;
        }

        dragIcon.sprite = icon.sprite;
        dragIcon.rectTransform.sizeDelta = new Vector2(60, 60);
        dragIcon.enabled = true;

        canvasGroup.alpha = 0.4f; // faded
    }

    // ============================
    // DRAGGING
    // ============================
    public void OnDrag(PointerEventData eventData)
    {
        if (dragIcon == null) return;

        dragIcon.rectTransform.position = eventData.position;
    }

    // ============================
    // DROP ON THIS SLOT
    // ============================
    public void OnDrop(PointerEventData eventData)
    {
        // Split-drag case
        if (splitDragAmount > 0 && splitDragItem != null)
        {
            // CASE 1: Dropping onto empty slot
            if (item == null)
            {
                item = splitDragItem;
                quantity = splitDragAmount;
                UpdateSlotUI();
                InventoryManager.Instance.SaveInventory();
            }
            // CASE 2: Dropping onto same item type with space
            else if (item == splitDragItem && quantity < item.maxStack)
            {
                int space = item.maxStack - quantity;
                int moveAmount = Mathf.Min(space, splitDragAmount);
                quantity += moveAmount;
                UpdateSlotUI();

                // If not all splitDragAmount could be merged, return the rest to original slot
                if (moveAmount < splitDragAmount)
                {
                    // Find the original slot and return the remainder
                    foreach (var slot in InventoryManager.Instance.itemSlot)
                    {
                        if (slot != null && slot != this && slot.item == splitDragItem && slot.quantity + moveAmount == (slot.quantity + splitDragAmount))
                        {
                            slot.quantity += (splitDragAmount - moveAmount);
                            slot.UpdateSlotUI();
                            break;
                        }
                    }
                }
                InventoryManager.Instance.SaveInventory();
            }
            // CASE 3: Dropping onto different item type (do nothing or swap, as you wish)
            // For now, do nothing

            // Clear split drag state globally
            splitDragAmount = 0;
            splitDragItem = null;
            splitOriginSlot = null;
            return;
        }


            if (draggedSlot == null || draggedSlot == this)
            return;


         //Only drag case not split
        // CASE 1 same item type + have space: merge stacks
        if (draggedSlot.item != null &&
           item != null &&
           draggedSlot.item == item &&
           quantity < item.maxStack)
        {
            int space = item.maxStack - quantity;
            int moveAmount = Mathf.Min(space, draggedSlot.quantity);

            quantity += moveAmount;
            draggedSlot.quantity -= moveAmount;

            if (draggedSlot.quantity == 0)
                draggedSlot.ClearSlot();

            UpdateSlotUI();
            draggedSlot.UpdateSlotUI();
        }
        else if (draggedSlot.item != null && item == null)
        {
            //CASE 2  Dropping onto an empty slot: just move the item
            item = draggedSlot.item;
            quantity = draggedSlot.quantity;
            draggedSlot.ClearSlot();

            UpdateSlotUI();
            draggedSlot.UpdateSlotUI();
        }
        else
        {
            // CASE 3 different item swap
            ItemObject tempItem = item;
            int tempQuantity = quantity;

            item = draggedSlot.item;
            quantity = draggedSlot.quantity;

            draggedSlot.item = tempItem;
            draggedSlot.quantity = tempQuantity;

            UpdateSlotUI();
            draggedSlot.UpdateSlotUI();
        }

        InventoryManager.Instance.SaveInventory(); // keep your save system
    }

    // ============================
    // DRAG END
    // ============================
    public void OnEndDrag(PointerEventData eventData)
    {
        if (dragIcon != null)
            dragIcon.enabled = false;

        canvasGroup.alpha = 1f;

        // If split drag was not dropped anywhere, return items to original slot
        if (splitDragAmount > 0 && splitDragItem != null)
        {
            quantity += splitDragAmount;
            UpdateSlotUI();
            splitDragAmount = 0;
            splitDragItem = null;
        }

        draggedSlot = null;
    }

    public int AddItem(ItemObject newItem, int amount)
    {
        if (item == null)
        {
            item = newItem;
            quantity = 0;
        }

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
            item = null;
            quantity = 0;
            return;
        }

        icon.enabled = true;
        icon.sprite = item.icon;
        quantityText.text = quantity.ToString();
        quantityText.enabled = true;
       
    }

    public void ClearSlot()
    {
        item = null;
        quantity = 0;

        if (icon != null)
            icon.enabled = false;
        if (quantityText != null)
            quantityText.enabled = false;
        if (selectedRect != null)
            selectedRect.SetActive(false);
    }


    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
            OnLeftClick();
    }


    public void OnLeftClick()
    {
        if (inventoryManager != null)
            inventoryManager.DeselectAllSlots();
        if (selectedRect != null)
            selectedRect.SetActive(true);
        thisItemSelected = true;

        if (item != null)
        {
            if (itemDescriptionNameText != null)
                itemDescriptionNameText.text = item.itemName;
            if (itemDescriptionDetailsText != null)
                itemDescriptionDetailsText.text = item.description;
            if (itemDescriptionImage != null)
                itemDescriptionImage.sprite = item.icon;
        }
        else
        {
            if (itemDescriptionNameText != null)
                itemDescriptionNameText.text = "";
            if (itemDescriptionDetailsText != null)
                itemDescriptionDetailsText.text = "";
            if (itemDescriptionImage != null)
                itemDescriptionImage.sprite = null;
        }
    }

    public void SplitStack(int splitAmount)
    {
        // Only split if there is more than 1 item and enough to split
        if (item == null || quantity <= 1 || splitAmount <= 0 || splitAmount >= quantity)
            return;

        // Find an empty slot in the inventory
        foreach (var slot in InventoryManager.Instance.itemSlot)
        {
            if (slot == null) continue;
            if (slot.item == null)
            {
                // Move splitAmount to the empty slot
                slot.item = item;
                slot.quantity = splitAmount;
                slot.UpdateSlotUI();

                // Reduce quantity in current slot
                quantity -= splitAmount;
                UpdateSlotUI();

                InventoryManager.Instance.SaveInventory();
                return;
            }
        }
    }

    // In ItemSlot.cs
public void UseItem()
{
    if (item == null || quantity <= 0) return;
    if (item.type == ItemType.Consumable && item.healAmount > 0f)
    {
        var playerHealth = GameObject.FindObjectOfType<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.Heal(item.healAmount);

            // Play heal sound effect
            AudioManager.Instance.PlayPlayerUseItemToHeal();

            quantity--;
            if (quantity <= 0) ClearSlot();
            UpdateSlotUI();
            InventoryManager.Instance.SaveInventory();
        }
    }
    // You can add more logic for buffs or other consumable effects here
}


}
