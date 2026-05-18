using UnityEngine;

public class CoinPickUp : MonoBehaviour
{
    [Header("Coin System")]
    public CoinSystem coin;

    [Header("Coin Settings")]
    [SerializeField] private int coinAmount = 1;

    [Header("Message UI")]
    [SerializeField] private CanvasMessageManager messageManager;
    [SerializeField] private bool findMessageManagerAutomatically = true;
    [SerializeField] private bool showPickupMessage = true;

    [Header("Pickup Message")]
    [SerializeField] private string pickupMessageFormat = "Picked up coin: +{0}";
    [SerializeField] private Color pickupMessageColor = Color.yellow;
    [SerializeField] private float customVisibleDuration = -1f;

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

        isPickedUp = true;

        if (coin == null)
        {
            Debug.LogWarning($"{name}: CoinSystem не назначен.");
            return;
        }

        coin.AddCoins(coinAmount);

        ShowPickupMessage();

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
            Debug.LogWarning("CoinPickUp: CanvasMessageManager не найден.");
            return;
        }

        string message = string.Format(pickupMessageFormat, coinAmount);

        messageManager.ShowMessage(message, pickupMessageColor, customVisibleDuration);
    }
}