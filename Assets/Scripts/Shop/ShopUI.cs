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

        MoneyManager.Instance.ShowMoneyUI();

        var player = GameObject.FindGameObjectWithTag("Player")?.GetComponent<PlayerMovement>();
        if (player) player.enabled = false;
        //stop running sfx
        AudioManager.Instance.StopPlayerRunning();
        //Open shop sfx
        AudioManager.Instance.PlayShopToggle();
        RefreshShopUI();
    }

    public void CloseShop()
    {
        shopPanel.SetActive(false);
        Time.timeScale = 1;

        MoneyManager.Instance.HideMoneyUI();

        var player = GameObject.FindGameObjectWithTag("Player")?.GetComponent<PlayerMovement>();
        if (player) player.enabled = true;
        //start sfx again
        AudioManager.Instance.StartPlayerRunning();
        //Close shop sfx
        AudioManager.Instance.PlayShopToggle();
    }
}
