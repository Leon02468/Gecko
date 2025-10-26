using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ItemSlot : MonoBehaviour
{
    //===ITEM DATA===//
    public string itemName;
    public int quantity;
    public Sprite itemSprite;
    public bool isFull;

    //===ITEM SLOT===//
    [SerializeField]
    private TMP_Text quantityText;

    [SerializeField]
    private Image itemImage;

    public void AddItem(string itemName, int quantity, Sprite itemSprite)
    {
        this.itemName = itemName;
        this.quantity = quantity;
        this.itemSprite = itemSprite;
        isFull = true;
        // Update UI Inventory Slot
        itemImage.sprite = itemSprite;
        itemImage.enabled = true;
        quantityText.text = quantity.ToString();
        quantityText.enabled = true;
    }
}
