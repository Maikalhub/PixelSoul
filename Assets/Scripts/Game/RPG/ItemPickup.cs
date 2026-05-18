using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    [Header("Item")]
    public ItemData item;

    [Header("Message UI")]
    [SerializeField] private CanvasMessageManager messageManager;
    [SerializeField] private bool findMessageManagerAutomatically = true;
    [SerializeField] private bool showPickupMessage = true;

    [Header("Pickup Message")]
    [SerializeField] private string pickupMessageFormat = "Picked up: {0}";
    [SerializeField] private Color pickupMessageColor = Color.white;
    [SerializeField] private float customVisibleDuration = -1f;

    [Header("Extra Info")]
    [SerializeField] private bool showItemType = true;
    [SerializeField] private bool showUseEffect = true;
    [SerializeField] private bool showDescription = false;

    private bool isPickedUp;

    private void Start()
    {
        if (messageManager == null && findMessageManagerAutomatically)
            messageManager = FindObjectOfType<CanvasMessageManager>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isPickedUp)
            return;

        if (!collision.CompareTag("Player"))
            return;

        if (item == null)
        {
            Debug.LogWarning($"{name}: ItemPickup не имеет ItemData.");
            return;
        }

        isPickedUp = true;

        ShowPickupMessage();

        Inventory.Instance.AddItem(item);

        Destroy(gameObject);
    }

    private void ShowPickupMessage()
    {
        if (!showPickupMessage)
            return;

        if (messageManager == null && findMessageManagerAutomatically)
            messageManager = FindObjectOfType<CanvasMessageManager>();

        if (messageManager == null)
        {
            Debug.LogWarning("ItemPickup: CanvasMessageManager не найден.");
            return;
        }

        string message = BuildPickupMessage();

        messageManager.ShowMessage(message, pickupMessageColor, customVisibleDuration);
    }

    private string BuildPickupMessage()
    {
        string itemName = string.IsNullOrWhiteSpace(item.itemName)
            ? item.name
            : item.itemName;

        string message = string.Format(pickupMessageFormat, itemName);

        if (showItemType)
            message += $" | Type: {item.itemType}";

        if (showUseEffect && item.useEffectType != ItemUseEffectType.None)
            message += $" | Effect: {GetUseEffectText()}";

        if (showDescription && !string.IsNullOrWhiteSpace(item.description))
            message += $" | {item.description}";

        return message;
    }

    private string GetUseEffectText()
    {
        string valueText = item.useValue.ToString("0.#");

        switch (item.useEffectType)
        {
            case ItemUseEffectType.Damage:
                return $"+{valueText} Damage";

            case ItemUseEffectType.Defense:
                return $"+{valueText} Defense";

            case ItemUseEffectType.MaxHealth:
                return $"+{valueText} Max Health";

            case ItemUseEffectType.RestoreHealth:
                return $"+{valueText} Health";

            case ItemUseEffectType.MaxStamina:
                return $"+{valueText} Max Stamina";

            case ItemUseEffectType.RestoreStamina:
                return $"+{valueText} Stamina";

            case ItemUseEffectType.MoveSpeed:
                return $"+{valueText} Move Speed{GetDurationText()}";

            case ItemUseEffectType.JumpPower:
                return $"+{valueText} Jump Power{GetDurationText()}";

            case ItemUseEffectType.SprintMultiplier:
                return $"+{valueText} Sprint Multiplier{GetDurationText()}";

            case ItemUseEffectType.DashSpeed:
                return $"+{valueText} Dash Speed{GetDurationText()}";

            default:
                return item.useEffectType.ToString();
        }
    }

    private string GetDurationText()
    {
        if (!item.isTemporaryBuff || item.buffDuration <= 0f)
            return "";

        return $" for {item.buffDuration:0.#}s";
    }
}