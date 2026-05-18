using UnityEngine;
using System.IO;

public class LevelProgressManager : MonoBehaviour
{
    public static LevelProgressManager Instance { get; private set; }

    [Header("ScriptableObject с прогрессом")]
    [SerializeField] private LevelProgressData progressData;

    [Header("Настройки сохранения")]
    [SerializeField] private string saveFileName = "level_progress.json";

    [Header("Настройки отладки")]
    [SerializeField] private bool logSaveLoad = true;

    private string savePath;

    public LevelProgressData ProgressData => progressData;

    private void Awake()
    {
        // Синглтон - не уничтожается при загрузке новых сцен
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        savePath = Path.Combine(Application.persistentDataPath, saveFileName);

        // Загружаем сохранения при старте
        LoadProgress();
    }

    // ================= СОХРАНЕНИЕ / ЗАГРУЗКА =================

    /// <summary>
    /// Сохраняет текущий прогресс в JSON
    /// </summary>
    public void SaveProgress()
    {
        if (progressData == null)
        {
            Debug.LogError("LevelProgressManager: progressData не назначен!");
            return;
        }

        string json = JsonUtility.ToJson(progressData, true);
        File.WriteAllText(savePath, json);

        if (logSaveLoad)
            Debug.Log($"Прогресс сохранён в: {savePath}");
    }

    /// <summary>
    /// Загружает прогресс из JSON. Если файла нет — создаёт новый с дефолтными значениями.
    /// </summary>
    public void LoadProgress()
    {
        if (progressData == null)
        {
            Debug.LogError("LevelProgressManager: progressData не назначен!");
            return;
        }

        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            JsonUtility.FromJsonOverwrite(json, progressData);

            if (logSaveLoad)
                Debug.Log($"Прогресс загружен из: {savePath}\nПоследний уровень: {progressData.lastLevelIndex}, Пройдено: {progressData.lastCompletedLevelIndex}");
        }
        else
        {
            if (logSaveLoad)
                Debug.Log("Файл сохранения не найден. Используются значения по умолчанию.");

            SaveProgress();
        }
    }

    /// <summary>
    /// Обновляет данные прогресса из текущего состояния LevelManager
    /// </summary>
    public void UpdateProgressFromLevelManager()
    {
        if (progressData == null)
        {
            Debug.LogError("LevelProgressManager: progressData не назначен!");
            return;
        }

        if (LevelManager.Instance == null)
        {
            Debug.LogWarning("LevelProgressManager: LevelManager.Instance не найден. Данные не обновлены.");
            return;
        }

        int currentIndex = LevelManager.Instance.CurrentLevelIndex;

        // Обновляем данные
        progressData.lastLevelIndex = currentIndex;

        // Синхронизируем массивы, если нужно
        int totalLevels = GetTotalLevelsCount();
        if (progressData.completedLevels == null || progressData.completedLevels.Length != totalLevels)
        {
            SyncArrays(totalLevels);
        }

        // Обновляем состояние текущего уровня
        if (IsValidIndex(currentIndex))
        {
            progressData.collectedKeysPerLevel[currentIndex] = LevelManager.Instance.GetCollectedKeys();
            progressData.totalCollectedKeys = CalculateTotalKeys();

            if (LevelManager.Instance.IsLevelCompleted(currentIndex))
            {
                progressData.completedLevels[currentIndex] = true;

                if (currentIndex > progressData.lastCompletedLevelIndex)
                {
                    progressData.lastCompletedLevelIndex = currentIndex;
                }
            }
        }

        SaveProgress();
    }

    // ================= ПУБЛИЧНЫЕ МЕТОДЫ =================

    /// <summary>
    /// Продолжить игру с последнего сохранённого уровня
    /// </summary>
    public void ContinueGame()
    {
        LoadProgress();

        if (progressData == null) return;

        int targetLevel = Mathf.Max(0, progressData.lastLevelIndex);

        if (logSaveLoad)
            Debug.Log($"Продолжаем игру с уровня: {targetLevel}");

        ApplyLevelToManager(targetLevel);
    }

    /// <summary>
    /// Начать новую игру (сброс прогресса)
    /// </summary>
    public void NewGame()
    {
        if (progressData == null) return;

        int totalLevels = GetTotalLevelsCount();
        progressData.InitializeForLevelCount(totalLevels);

        progressData.lastLevelIndex = 0;
        progressData.lastCompletedLevelIndex = -1;
        progressData.totalCollectedKeys = 0;
        progressData.totalPlayTime = 0f;

        SaveProgress();

        if (logSaveLoad)
            Debug.Log("Новая игра: прогресс сброшен");

        ApplyLevelToManager(0);
    }

    /// <summary>
    /// Загрузить первый уровень
    /// </summary>
    public void LoadFirstLevel()
    {
        ApplyLevelToManager(0);
    }

    /// <summary>
    /// Загрузить последний доступный уровень
    /// </summary>
    public void LoadLastAvailableLevel()
    {
        LoadProgress();

        if (progressData == null) return;

        int targetLevel = 0;

        if (progressData.lastCompletedLevelIndex >= 0)
        {
            targetLevel = Mathf.Min(progressData.lastCompletedLevelIndex + 1, GetTotalLevelsCount() - 1);
        }

        if (logSaveLoad)
            Debug.Log($"Загружаем последний доступный уровень: {targetLevel}");

        ApplyLevelToManager(targetLevel);
    }

    /// <summary>
    /// Загрузить уровень по индексу
    /// </summary>
    public void LoadLevelByIndex(int index)
    {
        ApplyLevelToManager(index);
    }

    /// <summary>
    /// Полный сброс прогресса
    /// </summary>
    public void ResetAllProgress()
    {
        if (progressData == null) return;

        int totalLevels = GetTotalLevelsCount();
        progressData.InitializeForLevelCount(totalLevels);

        progressData.lastLevelIndex = 0;
        progressData.lastCompletedLevelIndex = -1;
        progressData.totalCollectedKeys = 0;
        progressData.totalPlayTime = 0f;

        if (File.Exists(savePath))
            File.Delete(savePath);

        SaveProgress();

        Debug.Log("Весь прогресс сброшен");

        ApplyLevelToManager(0);
    }

    // ================= ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ =================

    private void ApplyLevelToManager(int index)
    {
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.SetCurrentLevelFromCode(index, true);
        }
        else
        {
            Debug.LogWarning("LevelProgressManager: LevelManager.Instance не найден. Уровень не применён.");
        }
    }

    private void SyncArrays(int totalLevels)
    {
        bool[] oldCompleted = progressData.completedLevels;
        int[] oldKeys = progressData.collectedKeysPerLevel;

        progressData.completedLevels = new bool[totalLevels];
        progressData.collectedKeysPerLevel = new int[totalLevels];

        for (int i = 0; i < totalLevels; i++)
        {
            if (oldCompleted != null && i < oldCompleted.Length)
                progressData.completedLevels[i] = oldCompleted[i];
            else
                progressData.completedLevels[i] = false;

            if (oldKeys != null && i < oldKeys.Length)
                progressData.collectedKeysPerLevel[i] = oldKeys[i];
            else
                progressData.collectedKeysPerLevel[i] = 0;
        }
    }

    private int CalculateTotalKeys()
    {
        if (progressData.collectedKeysPerLevel == null) return 0;

        int total = 0;
        foreach (int keys in progressData.collectedKeysPerLevel)
            total += keys;

        return total;
    }

    private int GetTotalLevelsCount()
    {
        // Пытаемся получить количество уровней из LevelManager
        if (LevelManager.Instance != null)
        {
            // Если LevelManager имеет публичный метод/свойство для количества уровней
            // return LevelManager.Instance.GetTotalLevels();
        }

        // Иначе возвращаем текущую длину массива, или 0
        return progressData.completedLevels != null ? progressData.completedLevels.Length : 0;
    }

    private bool IsValidIndex(int index)
    {
        return progressData.completedLevels != null &&
               index >= 0 &&
               index < progressData.completedLevels.Length;
    }
}