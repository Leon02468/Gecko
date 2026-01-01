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

        GameManager.Instance.MoneyManagerInstance.ShowMoneyUI();

        var player = GameObject.FindGameObjectWithTag("Player")?.GetComponent<PlayerMovement>();
        if (player) player.enabled = false;
        //stop running sfx
        GameManager.Instance.AudioInstance.StopPlayerRunning();
        //Open shop sfx
        GameManager.Instance.AudioInstance.PlayShopToggle();
        RefreshShopUI();
    }

    public void CloseShop()
    {
        shopPanel.SetActive(false);
        Time.timeScale = 1;

        GameManager.Instance.MoneyManagerInstance.HideMoneyUI();

        var player = GameObject.FindGameObjectWithTag("Player")?.GetComponent<PlayerMovement>();
        if (player) player.enabled = true;
        //start sfx again
        GameManager.Instance.AudioInstance.StartPlayerRunning();
        //Close shop sfx
        GameManager.Instance.AudioInstance.PlayShopToggle();
    }
}
