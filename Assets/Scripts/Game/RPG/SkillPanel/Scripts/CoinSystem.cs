using UnityEngine;
using TMPro;

public class CoinSystem : MonoBehaviour
{
    public static CoinSystem Instance;

    [Header("Coins")]
    public int CurrentCoins = 1;

    [Header("UI")]
    public TMP_Text coinText;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        UpdateUI(); // сразу показать текущее количество при запуске
    }

    public void AddCoins(int amount)
    {
        CurrentCoins += amount;
        UpdateUI();
    }

    public bool SpendCoins(int amount)
    {
        if (CurrentCoins >= amount)
        {
            CurrentCoins -= amount;
            UpdateUI(); // обновляем текст после траты
            return true;
        }

        return false;
    }

    public void UpdateUI()
    {
        if (coinText != null)
        {
            coinText.text = CurrentCoins.ToString();
        }
        else
        {
            Debug.LogWarning("CoinSystem: coinText не назначен в Inspector.");
        }
    }
}