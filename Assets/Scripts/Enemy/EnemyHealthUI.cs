using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

    [Header("Canvas UI (used when Follow Enemy = false)")]
    [SerializeField] private TMP_Text canvasNameText;
    [SerializeField] private Image canvasHealthImage;
    [SerializeField] private string customObjectName;

    [Header("Canvas Visibility")]
    [SerializeField] private bool showCanvasOnlyAfterLevelStart = true;

    [Header("Flash Settings")]
    [SerializeField] private bool flashOnDamage = true;
    [SerializeField] private Color flashColor = Color.red;
    [SerializeField] private float flashDuration = 0.1f;

    private SpriteRenderer spriteRenderer;
    private EnemyAI enemyAI;
    private int lastSpriteIndex = -1;
    private float maxHealth;
    private Color originalColor = Color.white;
    private bool isFlashing;
    private bool isCanvasMode;
    private bool canShowCanvas;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (targetEnemy == null)
            targetEnemy = GetComponentInParent<EnemyAI>();

        enemyAI = targetEnemy;

        if (enemyAI != null)
            maxHealth = Mathf.Max(1f, enemyAI.maxHealth);

        RefreshDisplayMode();
        UpdateNameDisplay();

        canShowCanvas = !showCanvasOnlyAfterLevelStart;

        if (isCanvasMode)
            SetCanvasActive(false);
    }

    private void Start()
    {
        if (showCanvasOnlyAfterLevelStart)
            canShowCanvas = true;

        if (isCanvasMode && enemyAI != null && enemyAI.gameObject.activeInHierarchy && canShowCanvas)
        {
            SetCanvasActive(true);
            UpdateHealthDisplay();
        }
    }

    private void LateUpdate()
    {
        if (healthSprites == null || healthSprites.Length == 0)
            return;

        bool newCanvasMode = !followEnemy;

        if (newCanvasMode != isCanvasMode)
        {
            RefreshDisplayMode();
            UpdateNameDisplay();
        }

        if (enemyAI == null)
        {
            if (isCanvasMode)
                SetCanvasActive(false);
            return;
        }

        if (!canShowCanvas)
        {
            if (isCanvasMode)
                SetCanvasActive(false);
            return;
        }

        if (!enemyAI.gameObject.activeInHierarchy)
        {
            if (isCanvasMode)
                SetCanvasActive(false);
            return;
        }
        else
        {
            if (isCanvasMode)
                SetCanvasActive(true);
        }

        if (followEnemy)
        {
            transform.position = enemyAI.transform.position + offset;

            if (ignoreEnemyFlip)
            {
                Vector3 scale = transform.localScale;
                scale.x = Mathf.Abs(scale.x);
                transform.localScale = scale;
            }
        }

        UpdateHealthDisplay();
    }

    private void RefreshDisplayMode()
    {
        isCanvasMode = !followEnemy;

        if (isCanvasMode)
        {
            if (spriteRenderer != null)
                spriteRenderer.enabled = false;

            if (canvasHealthImage != null)
                originalColor = canvasHealthImage.color;
            else
                originalColor = Color.white;
        }
        else
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = true;
                originalColor = spriteRenderer.color;
            }
            else
            {
                originalColor = Color.white;
            }
        }
    }

    private void SetCanvasActive(bool isActive)
    {
        if (canvasNameText != null)
            canvasNameText.gameObject.SetActive(isActive);

        if (canvasHealthImage != null)
            canvasHealthImage.gameObject.SetActive(isActive);
    }

    private void UpdateNameDisplay()
    {
        if (canvasNameText == null || enemyAI == null)
            return;

        if (!string.IsNullOrWhiteSpace(customObjectName))
            canvasNameText.text = customObjectName;
        else
            canvasNameText.text = enemyAI.gameObject.name;
    }

    private void UpdateHealthDisplay()
    {
        if (enemyAI == null || healthSprites == null || healthSprites.Length == 0)
            return;

        float currentHealth = Mathf.Max(0, enemyAI.health);
        int spriteIndex;

        if (currentHealth <= 0)
        {
            spriteIndex = healthSprites.Length - 1;
        }
        else if (useHealthPercentage)
        {
            float percent = currentHealth / maxHealth;
            spriteIndex = Mathf.FloorToInt((1f - percent) * (healthSprites.Length - 1));
        }
        else
        {
            float step = maxHealth / healthSprites.Length;
            spriteIndex = Mathf.FloorToInt((maxHealth - currentHealth) / step);
        }

        spriteIndex = Mathf.Clamp(spriteIndex, 0, healthSprites.Length - 1);

        if (spriteIndex != lastSpriteIndex)
        {
            SetHealthSprite(healthSprites[spriteIndex]);

            if (spriteIndex > lastSpriteIndex && flashOnDamage && !isFlashing)
                StartCoroutine(Flash());

            lastSpriteIndex = spriteIndex;
        }
    }

    private void SetHealthSprite(Sprite sprite)
    {
        if (isCanvasMode)
        {
            if (canvasHealthImage != null)
                canvasHealthImage.sprite = sprite;
        }
        else
        {
            if (spriteRenderer != null)
                spriteRenderer.sprite = sprite;
        }
    }

    private void SetDisplayColor(Color color)
    {
        if (isCanvasMode)
        {
            if (canvasHealthImage != null)
                canvasHealthImage.color = color;
        }
        else
        {
            if (spriteRenderer != null)
                spriteRenderer.color = color;
        }
    }

    private IEnumerator Flash()
    {
        isFlashing = true;

        for (int i = 0; i < 2; i++)
        {
            SetDisplayColor(flashColor);
            yield return new WaitForSeconds(flashDuration / 2f);

            SetDisplayColor(originalColor);
            yield return new WaitForSeconds(flashDuration / 2f);
        }

        SetDisplayColor(originalColor);
        isFlashing = false;
    }
}