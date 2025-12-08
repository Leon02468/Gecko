using UnityEngine;
using TMPro;

public class MoneyManager : MonoBehaviour
{
    public static MoneyManager Instance;
    public int Money { get; private set; }
    public GameObject moneyUI;
    public TMP_Text moneyText;

    private const string MoneyKey = "PlayerMoney";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            LoadMoney();
            moneyUI.SetActive(false);
            DontDestroyOnLoad(gameObject); // Persist across scenes
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AddMoney(int amount)
    {
        Money += amount;
        SaveMoney();
        UpdateMoneyUI();
        ShowMoneyUI();
        CancelInvoke(nameof(HideMoneyUI));
        Invoke(nameof(HideMoneyUI), 2f); // Hide after 2s
    }

    public void ShowMoneyUI()
    {
        moneyUI.SetActive(true);
        UpdateMoneyUI();
    }

    public void HideMoneyUI()
    {
        moneyUI.SetActive(false);
    }

    public void UpdateMoneyUI()
    {
        moneyText.text = $"{Money}";
    }

    public void SaveMoney()
    {
        PlayerPrefs.SetInt(MoneyKey, Money);
        PlayerPrefs.Save();
    }

    public void LoadMoney()
    {
        Money = PlayerPrefs.GetInt(MoneyKey, 0);
    }
}
