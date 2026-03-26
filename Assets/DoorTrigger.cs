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

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (LevelManager.Instance == null) return;

        LevelManager.Instance.TryUseDoor(targetLevelIndex, requireAllKeys, customSpawnPoint);
    }
}