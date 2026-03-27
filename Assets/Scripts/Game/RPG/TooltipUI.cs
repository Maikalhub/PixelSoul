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
    public TooltipPosition position = TooltipPosition.Bottom; // По умолчанию снизу курсора
    public Vector2 customOffset = new Vector2(0, -50);       // Используется если position = Custom

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
        return stats;
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

        // Выбираем смещение по типу позиции
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

        // Ограничение по Canvas
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