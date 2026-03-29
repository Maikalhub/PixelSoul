using UnityEngine;
using TMPro;

public class CoinSystem : MonoBehaviour
{
    public static CoinSystem Instance;
    private void Awake() => Instance = this;

    public int CurrentCoins = 1; // Начальное количество
    public TMP_Text coinText;

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
            return true;
        }
        return false;
    }

    public void UpdateUI()
    {
        if (coinText != null)
            coinText.text = $"{CurrentCoins}";
    }
}