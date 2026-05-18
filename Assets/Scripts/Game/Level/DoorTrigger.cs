using UnityEngine;

public class DoorTrigger : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";

    [Header("В какой уровень ведёт эта дверь")]
    [SerializeField] private int targetLevelIndex = 0;

    [Header("Нужно ли собрать все ключи перед входом")]
    [SerializeField] private bool requireAllKeys = true;

    [Header("Необязательно: своя точка появления после входа в эту дверь")]
    [SerializeField] private Transform customSpawnPoint;

    [Header("Клавиша взаимодействия")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    [Header("Спрайт подсказки над дверью")]
    [SerializeField] private GameObject interactionSprite;

    [Header("Message UI")]
    [SerializeField] private CanvasMessageManager messageManager;
    [SerializeField] private bool findMessageManagerAutomatically = true;
    [SerializeField] private bool showDoorMessages = true;

    [Header("Door Messages")]
    [SerializeField] private bool showMessageOnEnter = true;
    [SerializeField] private bool showMessageOnInteract = true;
    [SerializeField] private bool showKeyRequirementInEnterMessage = true;

    [SerializeField] private string enterMessageFormat = "Press {0} to enter the door";
    [SerializeField] private string enterMessageWithKeysFormat = "Press {0} to enter the door | Requires all keys";
    [SerializeField] private string interactMessageFormat = "Trying to enter level...";
    [SerializeField] private string levelManagerMissingMessage = "The door does not respond...";

    [Header("Message Colors")]
    [SerializeField] private Color enterMessageColor = Color.white;
    [SerializeField] private Color interactMessageColor = Color.white;
    [SerializeField] private Color errorMessageColor = Color.red;

    [Header("Message Duration")]
    [SerializeField] private float enterMessageDuration = 1.5f;
    [SerializeField] private float interactMessageDuration = 1.2f;
    [SerializeField] private float errorMessageDuration = 1.5f;

    private bool playerInsideTrigger = false;
    private Collider2D currentPlayer;

    private void Start()
    {
        if (interactionSprite != null)
            interactionSprite.SetActive(false);

        if (messageManager == null && findMessageManagerAutomatically)
            messageManager = FindObjectOfType<CanvasMessageManager>();
    }

    private void Update()
    {
        if (!playerInsideTrigger) return;
        if (currentPlayer == null) return;

        if (Input.GetKeyDown(interactKey))
            TryUseDoor();
    }

    private void TryUseDoor()
    {
        if (LevelManager.Instance == null)
        {
            ShowMessage(levelManagerMissingMessage, errorMessageColor, errorMessageDuration);
            Debug.LogWarning("DoorTrigger: LevelManager.Instance не найден.");
            return;
        }

        if (showMessageOnInteract)
        {
            string message = string.Format(interactMessageFormat, targetLevelIndex);
            ShowMessage(message, interactMessageColor, interactMessageDuration);
        }

        LevelManager.Instance.TryUseDoor(targetLevelIndex, requireAllKeys, customSpawnPoint);
    }

    private void ShowEnterMessage()
    {
        if (!showMessageOnEnter)
            return;

        string message;

        if (requireAllKeys && showKeyRequirementInEnterMessage)
            message = string.Format(enterMessageWithKeysFormat, interactKey);
        else
            message = string.Format(enterMessageFormat, interactKey);

        ShowMessage(message, enterMessageColor, enterMessageDuration);
    }

    private void ShowMessage(string message, Color color, float duration)
    {
        if (!showDoorMessages)
            return;

        if (string.IsNullOrWhiteSpace(message))
            return;

        if (messageManager == null && findMessageManagerAutomatically)
            messageManager = FindObjectOfType<CanvasMessageManager>();

        if (messageManager == null)
        {
            Debug.LogWarning("DoorTrigger: CanvasMessageManager не найден.");
            return;
        }

        messageManager.ShowMessage(message, color, duration);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;

        playerInsideTrigger = true;
        currentPlayer = other;

        if (interactionSprite != null)
            interactionSprite.SetActive(true);

        ShowEnterMessage();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;

        if (currentPlayer == other)
        {
            playerInsideTrigger = false;
            currentPlayer = null;

            if (interactionSprite != null)
                interactionSprite.SetActive(false);
        }
    }
}