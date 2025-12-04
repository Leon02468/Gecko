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

        draggedSlot = this;

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
        if (draggedSlot == null || draggedSlot == this)
            return;

        // CASE 1 Å® same item type Å® merge stacks
        if (draggedSlot.item != null && item != null && draggedSlot.item == item)
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
        else
        {
            // CASE 2 Å® different item Å® swap
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
        draggedSlot = null;
    }

    // ============================
    // (YOUR EXISTING CODE BELOW)
    // ============================

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

        icon.enabled = false;
        quantityText.enabled = false;
        selectedRect.SetActive(false);
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
            itemDescriptionNameText.text = item.itemName;
            itemDescriptionDetailsText.text = item.description;
            itemDescriptionImage.sprite = item.icon;
        }
        else
        {
            itemDescriptionNameText.text = "";
            itemDescriptionDetailsText.text = "";
            itemDescriptionImage.sprite = null;
        }
    }
}
