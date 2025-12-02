using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ShopSlot : MonoBehaviour
{
    public Image icon;
    public TMP_Text itemNameText;
    public TMP_Text priceText;
    public Button buyButton;

    private ItemObject item;
    private int price;

    public void SetSlot(ItemObject newItem, int newPrice)
    {
        item = newItem;
        price = newPrice;

        if (item != null)
        {
            icon.enabled = true;
            icon.sprite = item.icon;

            itemNameText.text = item.itemName;
            priceText.text = newPrice.ToString();
            buyButton.interactable = true;

            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(() =>
            {
                BuyItem();
            });
        }
        else
        {
            ClearSlot();
        }
    }

    public void ClearSlot()
    {
        item = null;
        icon.enabled = false;
        itemNameText.text = "";
        priceText.text = "";
        buyButton.interactable = false;
        buyButton.onClick.RemoveAllListeners();
    }

    void BuyItem()
    {
        if (item == null) return;

        InventoryManager.Instance.AddItem(item, 1);
        Debug.Log("Bought: " + item.itemName);
    }
}
