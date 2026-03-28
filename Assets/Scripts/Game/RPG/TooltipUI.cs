using UnityEngine;
using TMPro;

[RequireComponent(typeof(RectTransform))]
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

    [Header("Canvas Reference (optional)")]
    public Canvas canvas;

    [Header("Tooltip Positioning")]
    public TooltipPosition position = TooltipPosition.Bottom;
    public Vector2 customOffset = new Vector2(0, -50);

    public static TooltipUI Instance;

    private RectTransform rectTransform;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        rectTransform = GetComponent<RectTransform>();

        if (canvas == null)
        {
            canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("TooltipUI: Canvas не найден! Поместите TooltipUI внутрь Canvas или назначьте вручную.");
            }
        }

        gameObject.SetActive(false);
    }

    public void Show(ItemData item)
    {
        if (item == null) return;

        gameObject.SetActive(true);

        nameText.text = item.itemName;
        typeText.text = item.itemType.ToString();
        descriptionText.text = item.description;
        statsText.text = GetStatsText(item);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
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
            case ItemUseEffectType.Damage: effectName = "Buff Damage"; break;
            case ItemUseEffectType.Defense: effectName = "Buff Defense"; break;
            case ItemUseEffectType.MaxHealth: effectName = "Buff Max HP"; break;
            case ItemUseEffectType.RestoreHealth: effectName = "Heal"; break;
            case ItemUseEffectType.MaxStamina: effectName = "Buff Max Stamina"; break;
            case ItemUseEffectType.RestoreStamina: effectName = "Restore Stamina"; break;
            case ItemUseEffectType.MoveSpeed: effectName = "Buff Move Speed"; break;
            case ItemUseEffectType.JumpPower: effectName = "Buff Jump"; break;
            case ItemUseEffectType.SprintMultiplier: effectName = "Buff Sprint"; break;
            case ItemUseEffectType.DashSpeed: effectName = "Buff Dash"; break;
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

    void Update()
    {
        if (!gameObject.activeSelf || canvas == null || rectTransform == null) return;

        Vector2 localPoint;
        RectTransform canvasRect = canvas.transform as RectTransform;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            Input.mousePosition,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out localPoint
        );

        Vector2 offset = Vector2.zero;
        switch (position)
        {
            case TooltipPosition.Top: offset = new Vector2(0, rectTransform.rect.height / 2); break;
            case TooltipPosition.Bottom: offset = new Vector2(0, -rectTransform.rect.height / 2); break;
            case TooltipPosition.Left: offset = new Vector2(-rectTransform.rect.width / 2, 0); break;
            case TooltipPosition.Right: offset = new Vector2(rectTransform.rect.width / 2, 0); break;
            case TooltipPosition.Custom: offset = customOffset; break;
        }

        Vector2 anchoredPos = localPoint + offset;

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

        rectTransform.localPosition = anchoredPos;
    }
}
