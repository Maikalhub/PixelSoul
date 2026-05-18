using UnityEngine;

[CreateAssetMenu(fileName = "LevelProgressData", menuName = "Level Progress/Level Progress Data")]
public class LevelProgressData : ScriptableObject
{
    [Header("Прогресс прохождения")]
    [Tooltip("Индекс последнего уровня, на котором был игрок")]
    public int lastLevelIndex = 0;

    [Tooltip("Индекс последнего пройденного уровня")]
    public int lastCompletedLevelIndex = -1;

    [Tooltip("Массив: пройден ли уровень (true/false)")]
    public bool[] completedLevels = new bool[0];

    [Tooltip("Массив: сколько ключей собрано на каждом уровне")]
    public int[] collectedKeysPerLevel = new int[0];

    [Tooltip("Общее количество собранных ключей")]
    public int totalCollectedKeys = 0;

    [Tooltip("Время игры в секундах")]
    public float totalPlayTime = 0f;

    /// <summary>
    /// Инициализирует массивы под нужное количество уровней
    /// </summary>
    public void InitializeForLevelCount(int levelCount)
    {
        if (levelCount <= 0) return;

        completedLevels = new bool[levelCount];
        collectedKeysPerLevel = new int[levelCount];

        for (int i = 0; i < levelCount; i++)
        {
            completedLevels[i] = false;
            collectedKeysPerLevel[i] = 0;
        }
    }
}