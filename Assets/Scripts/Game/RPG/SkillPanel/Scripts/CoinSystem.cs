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
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
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

    /// <summary>
    /// Устанавливает количество монет (для восстановления после рестарта)
    /// </summary>
    public void SetCoins(int amount)
    {
        CurrentCoins = Mathf.Max(0, amount);
        RefreshAllUI();
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