using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Health UI")]
    public Image healthImage;              // Image вместо SpriteRenderer
    public Sprite[] healthSprites;         // спрайты (0 ? пусто, последний ? фул)
    [Range(0f, 1f)] public float healthBlinkThreshold = 0.25f;
    public float healthBlinkSpeed = 5f;

    [Header("Stamina UI")]
    public Image staminaImage;             // Image вместо SpriteRenderer
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
        if (healthImage == null || healthSprites.Length == 0) return;

        float healthPercent = (float)player.currentHealth / player.maxHealth;
        int spriteIndex = Mathf.RoundToInt(healthPercent * (healthSprites.Length - 1));

        healthImage.sprite = healthSprites[spriteIndex];

        // Мигание
        if (healthPercent <= healthBlinkThreshold)
        {
            float alpha = Mathf.Abs(Mathf.Sin(Time.time * healthBlinkSpeed));
            Color c = healthImage.color;
            c.a = alpha;
            healthImage.color = c;
        }
        else
        {
            Color c = healthImage.color;
            c.a = 1f;
            healthImage.color = c;
        }
    }

    void UpdateStaminaUI()
    {
        if (staminaImage == null) return;

        float staminaPercent = (float)player.currentStamina / player.maxStamina;
        Color targetColor = Color.Lerp(staminaEmptyColor, staminaFullColor, staminaPercent);

        // Мигание
        if (staminaPercent <= staminaBlinkThreshold)
        {
            float blink = Mathf.Abs(Mathf.Sin(Time.time * staminaBlinkSpeed));
            targetColor = Color.Lerp(staminaEmptyColor, staminaFullColor, blink);
        }

        staminaImage.color = targetColor;
    }
}