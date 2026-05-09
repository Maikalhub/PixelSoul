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

    private bool playerInsideTrigger = false;
    private Collider2D currentPlayer;

    private void Start()
    {
        if (interactionSprite != null)
        {
            interactionSprite.SetActive(false);
        }
    }

    private void Update()
    {
        if (!playerInsideTrigger) return;
        if (currentPlayer == null) return;
        if (LevelManager.Instance == null) return;

        if (Input.GetKeyDown(interactKey))
        {
            LevelManager.Instance.TryUseDoor(targetLevelIndex, requireAllKeys, customSpawnPoint);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;

        playerInsideTrigger = true;
        currentPlayer = other;

        if (interactionSprite != null)
        {
            interactionSprite.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;

        if (currentPlayer == other)
        {
            playerInsideTrigger = false;
            currentPlayer = null;

            if (interactionSprite != null)
            {
                interactionSprite.SetActive(false);
            }
        }
    }
}