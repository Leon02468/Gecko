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
            moneyUI.SetActive(false);
            DontDestroyOnLoad(gameObject); // Persist across scenes
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public int GetMoneySnapshot() => Money;
    public void ApplyMoneySnapshot(int amount) => SetMoney(amount);

    public void AddMoney(int amount)
    {
        Money += amount;
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

    public void SetMoney(int amount)
    {
        Money = amount;
        UpdateMoneyUI();
    }

}
