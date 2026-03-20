using UnityEngine;

public class EnemyHealthUI : MonoBehaviour
{
    [Header("Health Sprite Configuration")]
    [SerializeField] private Sprite[] healthSprites;
    [SerializeField] private bool useHealthPercentage = true;

    [Header("Target Enemy")]
    [SerializeField] private EnemyAI targetEnemy;

    [Header("Position Settings")]
    [SerializeField] private Vector3 offset = new Vector3(0, 1f, 0);
    [SerializeField] private bool followEnemy = true;
    [SerializeField] private bool ignoreEnemyFlip = true;

    [Header("Flash Settings")]
    [SerializeField] private bool flashOnDamage = true;
    [SerializeField] private Color flashColor = Color.red;
    [SerializeField] private float flashDuration = 0.1f;

    private SpriteRenderer spriteRenderer;
    private EnemyAI enemyAI;
    private int lastSpriteIndex = -1;
    private float maxHealth;
    private Color originalColor;
    private bool isFlashing;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;

        if (targetEnemy == null)
            targetEnemy = GetComponentInParent<EnemyAI>();

        enemyAI = targetEnemy;

        if (enemyAI != null)
            maxHealth = enemyAI.health;
    }

    private void LateUpdate()
    {
        if (enemyAI == null) return;

        if (followEnemy)
        {
            transform.position = enemyAI.transform.position + offset;
        }

        // ?? Фикс поворота
        if (ignoreEnemyFlip)
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x);
            transform.localScale = scale;
        }

        UpdateHealthDisplay();
    }

    private void UpdateHealthDisplay()
    {
        if (enemyAI.health <= 0)
        {
            spriteRenderer.sprite = healthSprites[healthSprites.Length - 1];
            return;
        }

        float currentHealth = Mathf.Max(0, enemyAI.health);
        int spriteIndex;

        if (useHealthPercentage)
        {
            float percent = currentHealth / maxHealth;
            spriteIndex = Mathf.FloorToInt((1 - percent) * (healthSprites.Length - 1));
        }
        else
        {
            float step = maxHealth / healthSprites.Length;
            spriteIndex = Mathf.FloorToInt((maxHealth - currentHealth) / step);
        }

        spriteIndex = Mathf.Clamp(spriteIndex, 0, healthSprites.Length - 1);

        if (spriteIndex != lastSpriteIndex)
        {
            spriteRenderer.sprite = healthSprites[spriteIndex];

            if (spriteIndex > lastSpriteIndex && flashOnDamage && !isFlashing)
                StartCoroutine(Flash());

            lastSpriteIndex = spriteIndex;
        }
    }

    private System.Collections.IEnumerator Flash()
    {
        isFlashing = true;

        for (int i = 0; i < 2; i++)
        {
            spriteRenderer.color = flashColor;
            yield return new WaitForSeconds(flashDuration / 2);

            spriteRenderer.color = originalColor;
            yield return new WaitForSeconds(flashDuration / 2);
        }

        spriteRenderer.color = originalColor;
        isFlashing = false;
    }
}