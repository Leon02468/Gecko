using UnityEngine;

public class ShopUI : MonoBehaviour
{
    [System.Serializable]
    public class ShopItem
    {
        public ItemObject item;
        public int price;
    }

    [Header("Shop Settings")]
    public ShopItem[] itemsForSale;

    [Header("UI")]
    public GameObject shopPanel;
    public ShopSlot[] shopSlots; // Å© drag ShopItemSlots here

    private void Start()
    {
        shopPanel.SetActive(false);
        RefreshShopUI();
    }

    public void RefreshShopUI()
    {
        for (int i = 0; i < shopSlots.Length; i++)
        {
            if (i < itemsForSale.Length)
            {
                shopSlots[i].SetSlot(itemsForSale[i].item, itemsForSale[i].price);
            }
            else
            {
                shopSlots[i].ClearSlot();
            }
        }
    }

    public void OpenShop()
    {
        shopPanel.SetActive(true);
        Time.timeScale = 0;

        var player = GameObject.FindGameObjectWithTag("Player")?.GetComponent<PlayerMovement>();
        if (player) player.enabled = false;

        RefreshShopUI();
    }

    public void CloseShop()
    {
        shopPanel.SetActive(false);
        Time.timeScale = 1;

        var player = GameObject.FindGameObjectWithTag("Player")?.GetComponent<PlayerMovement>();
        if (player) player.enabled = true;
    }
}
