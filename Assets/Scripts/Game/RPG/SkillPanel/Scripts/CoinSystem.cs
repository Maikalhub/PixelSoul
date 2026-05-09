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
        RefreshAllUI();
    }

    public void AddCoins(int amount)
    {
        CurrentCoins += amount;
        RefreshAllUI();
    }

    public bool SpendCoins(int amount)
    {
        if (CurrentCoins < amount)
            return false;

        CurrentCoins -= amount;
        RefreshAllUI();
        return true;
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

    private void RefreshAllUI()
    {
        UpdateUI();

        if (SkillTree.skillTree != null)
        {
            SkillTree.skillTree.UpdateAllSkillUI();
        }
    }
}