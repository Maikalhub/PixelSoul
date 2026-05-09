using UnityEngine;
using TMPro;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
public class TooltipUI : MonoBehaviour
{
    public enum TooltipPosition
    {
        Top,
        Bottom,
        Left,
        Right,
        Custom
    }

    [Header("Tooltip Elements")]
    public TMP_Text nameText;
    public TMP_Text typeText;
    public TMP_Text descriptionText;
    public TMP_Text statsText;

    [Header("Canvas Reference")]
    public Canvas canvas;

    [Header("Tooltip Positioning")]
    public TooltipPosition position = TooltipPosition.Bottom;
    public Vector2 customOffset = new Vector2(0, -50);

    [Header("Clamp To Screen")]
    public bool clampToCanvas = true;

    public static TooltipUI Instance;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();

        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }

    public void Show(ItemData item)
    {
        if (item == null) return;
        if (canvas == null) return;

        // Включаем объект, но пока невидимо
        gameObject.SetActive(true);
        canvasGroup.alpha = 0f;

        // Сначала заполняем текст
        nameText.text = item.itemName;
        typeText.text = item.itemType.ToString();
        descriptionText.text = item.description;
        statsText.text = GetStatsText(item);

        // Принудительно обновляем размеры UI
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);

        // Сразу ставим правильную позицию
        UpdatePosition();

        // И только теперь показываем
        canvasGroup.alpha = 1f;
    }

    public void Hide()
    {
        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (!gameObject.activeSelf) return;
        if (canvas == null || rectTransform == null) return;

        UpdatePosition();
    }

    private void UpdatePosition()
    {
        RectTransform canvasRect = canvas.transform as RectTransform;
        if (canvasRect == null) return;

        Vector2 localPoint;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            Input.mousePosition,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out localPoint
        );

        Vector2 offset = GetOffset();
        Vector2 anchoredPos = localPoint + offset;

        if (clampToCanvas)
        {
            Vector2 minPosition = new Vector2(
                -canvasRect.rect.width / 2 + rectTransform.rect.width / 2,
                -canvasRect.rect.height / 2 + rectTransform.rect.height / 2
            );

            Vector2 maxPosition = new Vector2(
                canvasRect.rect.width / 2 - rectTransform.rect.width / 2,
                canvasRect.rect.height / 2 - rectTransform.rect.height / 2
            );

            anchoredPos.x = Mathf.Clamp(anchoredPos.x, minPosition.x, maxPosition.x);
            anchoredPos.y = Mathf.Clamp(anchoredPos.y, minPosition.y, maxPosition.y);
        }

        // Для UI лучше anchoredPosition, а не localPosition
        rectTransform.anchoredPosition = anchoredPos;
    }

    private Vector2 GetOffset()
    {
        switch (position)
        {
            case TooltipPosition.Top:
                return new Vector2(0, rectTransform.rect.height / 2);

            case TooltipPosition.Bottom:
                return new Vector2(0, -rectTransform.rect.height / 2);

            case TooltipPosition.Left:
                return new Vector2(-rectTransform.rect.width / 2, 0);

            case TooltipPosition.Right:
                return new Vector2(rectTransform.rect.width / 2, 0);

            case TooltipPosition.Custom:
                return customOffset;
        }

        return Vector2.zero;
    }

    private string GetStatsText(ItemData item)
    {
        string stats = "";

        if (item.damage > 0) stats += $"Damage: {item.damage}\n";
        if (item.defense > 0) stats += $"Defense: {item.defense}\n";
        if (item.health > 0) stats += $"HP: {item.health}\n";

        string useText = GetUseText(item);

        if (!string.IsNullOrEmpty(useText))
            stats += useText;

        return stats.TrimEnd();
    }

    private string GetUseText(ItemData item)
    {
        if (item == null || !item.canUse || item.useEffectType == ItemUseEffectType.None)
            return "";

        string effectName = "";

        switch (item.useEffectType)
        {
            case ItemUseEffectType.Damage:
                effectName = "Buff Damage";
                break;

            case ItemUseEffectType.Defense:
                effectName = "Buff Defense";
                break;

            case ItemUseEffectType.MaxHealth:
                effectName = "Buff Max HP";
                break;

            case ItemUseEffectType.RestoreHealth:
                effectName = "Heal";
                break;

            case ItemUseEffectType.MaxStamina:
                effectName = "Buff Max Stamina";
                break;

            case ItemUseEffectType.RestoreStamina:
                effectName = "Restore Stamina";
                break;

            case ItemUseEffectType.MoveSpeed:
                effectName = "Buff Move Speed";
                break;

            case ItemUseEffectType.JumpPower:
                effectName = "Buff Jump";
                break;

            case ItemUseEffectType.SprintMultiplier:
                effectName = "Buff Sprint";
                break;

            case ItemUseEffectType.DashSpeed:
                effectName = "Buff Dash";
                break;
        }

        string valueText = item.useValue.ToString("0.##");

        bool instant =
            item.useEffectType == ItemUseEffectType.RestoreHealth ||
            item.useEffectType == ItemUseEffectType.RestoreStamina;

        if (instant)
            return $"{effectName}: {valueText}\n";

        if (item.isTemporaryBuff)
            return $"{effectName}: +{valueText} ({item.buffDuration:0.##} sec)\n";

        return $"{effectName}: +{valueText} (permanent)\n";
    }
}