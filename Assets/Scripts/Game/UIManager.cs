using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("Health UI")]
    public SpriteRenderer healthRenderer;       // Спрайт для здоровья
    public Sprite[] healthSprites;              // Массив спрайтов от пустого до полного (10 шт)
    [Range(0f, 1f)] public float healthBlinkThreshold = 0.25f; // порог мигания
    public float healthBlinkSpeed = 5f;        // скорость мигания

    [Header("Stamina UI")]
    public SpriteRenderer staminaBar;           // один спрайт, цвет меняется
    public Color staminaFullColor = Color.green;
    public Color staminaEmptyColor = Color.red;
    [Range(0f, 1f)] public float staminaBlinkThreshold = 0.25f;
    public float staminaBlinkSpeed = 5f;

    [Header("Stats Reference")]
    public PlayerMovement player;

    void Update()
    {
        if (player == null) return;

        UpdateHealthUI();
        UpdateStaminaUI();
    }

    void UpdateHealthUI()
    {
        if (healthRenderer == null || healthSprites.Length == 0) return;

        float healthPercent = (float)player.currentHealth / player.maxHealth;
        int spriteIndex = Mathf.RoundToInt(healthPercent * (healthSprites.Length - 1));
        healthRenderer.sprite = healthSprites[spriteIndex];

        // Мигание, если здоровье ниже порога
        if (healthPercent <= healthBlinkThreshold)
        {
            float alpha = Mathf.Abs(Mathf.Sin(Time.time * healthBlinkSpeed));
            Color c = healthRenderer.color;
            c.a = alpha;
            healthRenderer.color = c;
        }
        else
        {
            Color c = healthRenderer.color;
            c.a = 1f;
            healthRenderer.color = c;
        }
    }

    void UpdateStaminaUI()
    {
        if (staminaBar == null) return;

        float staminaPercent = (float)player.currentStamina / player.maxStamina;
        Color targetColor = Color.Lerp(staminaEmptyColor, staminaFullColor, staminaPercent);

        // Мигание при критическом уровне
        if (staminaPercent <= staminaBlinkThreshold)
        {
            float blink = Mathf.Abs(Mathf.Sin(Time.time * staminaBlinkSpeed));
            targetColor = Color.Lerp(staminaEmptyColor, staminaFullColor, blink);
        }

        staminaBar.color = targetColor;
    }
}
