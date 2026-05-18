using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Cinemachine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [System.Serializable]
    public class LevelData
    {
        [Header("Имя уровня / зоны")]
        public string levelName;

        [Header("Сколько ключей нужно собрать на этом уровне")]
        [Min(0)] public int keysOnLevel = 3;

        [Header("Куда перенести игрока при входе на этот уровень по умолчанию")]
        public Transform spawnPoint;

        [Header("Confiner для этого уровня")]
        public Collider2D levelConfinerShape;

        [Header("Какие объекты выключить при входе на этот уровень")]
        public GameObject[] objectsToDisableOnEnter;

        [Header("Какие объекты включить при входе на этот уровень")]
        public GameObject[] objectsToEnableOnEnter;

        [Header("Audio")]
        public AudioClip[] backgroundMusicClips;
        public AudioClip[] randomAmbientClips;

        [Tooltip("Минимальная и максимальная задержка между случайными звуками")]
        public Vector2 ambientRandomDelayRange = new Vector2(8f, 18f);

        [Tooltip("Диапазон громкости случайных ambient-звуков")]
        public Vector2 ambientRandomVolumeRange = new Vector2(0.25f, 0.6f);

        [Header("Блокировка уровня")]
        [Tooltip("Если включено, уровень откроется только после прохождения указанных уровней")]
        public bool lockedUntilRequiredLevelsCompleted = false;

        [Tooltip("Индексы уровней из массива Levels, которые должны быть пройдены")]
        public int[] requiredCompletedLevelIndices;
    }

    [Header("Игрок")]
    [SerializeField] private Transform player;

    [Header("Текущий уровень")]
    [Tooltip("Текущий уровень. Можно менять вручную в Inspector. Индекс берётся из массива Levels.")]
    [SerializeField, Min(0)] private int currentLevelIndex = 0;

    [Tooltip("Запасной Confiner. Используется только если у текущего уровня не назначен levelConfinerShape.")]
    [SerializeField] private Collider2D firstConfinerShape;

    [Header("Все уровни по порядку")]
    [SerializeField] private LevelData[] levels;

    [Header("Cinemachine")]
    [SerializeField] private CinemachineConfiner2D confiner2D;

    [Header("UI ключей")]
    [SerializeField] private Transform keyBarRoot;
    [SerializeField] private Image keyIconPrefab;
    [SerializeField] private float keySpacing = 40f;
    [SerializeField] private Color defaultKeyColor = Color.gray;
    [SerializeField] private Color collectedKeyColor = Color.white;

    [Header("UI названия уровня")]
    [SerializeField] private GameObject levelTitleRoot;
    [SerializeField] private TypewriterTextUI levelTitleTypewriter;

    [Tooltip("{0} заменится на имя текущего уровня")]
    [SerializeField] private string levelTitleFormat = "Уровень: {0}";

    [Header("Fade")]
    [SerializeField] private Image fadeImage;

    [Tooltip("Скорость затемнения и проявления экрана")]
    [SerializeField] private float fadeDuration = 0.35f;

    [Tooltip("Сколько секунд экран остается черным после переноса игрока на новый уровень")]
    [SerializeField] private float blackScreenHoldDuration = 0.5f;

    private readonly List<Image> spawnedKeyIcons = new List<Image>();

    private Rigidbody2D playerRb;

    private int collectedKeys;
    private bool isTransitioning;

    private bool[] completedLevelStates;
    private int[] collectedKeysByLevel;

    private LevelData CurrentLevel
    {
        get
        {
            if (levels == null) return null;
            if (currentLevelIndex < 0) return null;
            if (currentLevelIndex >= levels.Length) return null;

            return levels[currentLevelIndex];
        }
    }

    public bool HasAllKeys => CurrentLevel != null && collectedKeys >= CurrentLevel.keysOnLevel;
    public bool IsTransitioning => isTransitioning;
    public int CurrentLevelIndex => currentLevelIndex;
    public int CollectedKeys => collectedKeys;

    // Оставлено для совместимости с другими скриптами.
    // Сохранения больше нет, поэтому всегда false.
    public bool RememberProgress => false;

    private void OnValidate()
    {
        if (currentLevelIndex < 0)
            currentLevelIndex = 0;

        if (levels != null && levels.Length > 0)
            currentLevelIndex = Mathf.Clamp(currentLevelIndex, 0, levels.Length - 1);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (levels == null || levels.Length == 0)
        {
            Debug.LogError("LevelManager: массив levels пуст.");
            enabled = false;
            return;
        }

        if (player == null)
        {
            Debug.LogError("LevelManager: не назначен player.");
            enabled = false;
            return;
        }

        playerRb = player.GetComponent<Rigidbody2D>();

        InitializeRuntimeLevelProgress();

        // Если есть сохранённый прогресс — загружаем последний уровень
        if (LevelProgressManager.Instance != null && LevelProgressManager.Instance.ProgressData != null)
        {
            int savedLevel = LevelProgressManager.Instance.ProgressData.lastLevelIndex;
            if (savedLevel >= 0 && savedLevel < levels.Length)
            {
                currentLevelIndex = savedLevel;
                Debug.Log($"LevelManager: Загружен сохранённый уровень {savedLevel}");
            }
        }

        currentLevelIndex = Mathf.Clamp(currentLevelIndex, 0, levels.Length - 1);

        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
        }

        ApplyLevelData(true);
        ShowCurrentLevelTitle();
    }

    private void InitializeRuntimeLevelProgress()
    {
        completedLevelStates = new bool[levels.Length];
        collectedKeysByLevel = new int[levels.Length];

        for (int i = 0; i < levels.Length; i++)
        {
            completedLevelStates[i] = false;
            collectedKeysByLevel[i] = 0;
        }
    }

    public void CollectKey(GameObject keyObject)
    {
        if (isTransitioning)
            return;

        if (CurrentLevel == null)
            return;

        if (collectedKeys >= CurrentLevel.keysOnLevel)
            return;

        collectedKeys++;

        StoreCollectedKeysForCurrentLevel();

        UpdateKeyUI();

        if (keyObject != null)
            Destroy(keyObject);

        if (collectedKeys >= CurrentLevel.keysOnLevel)
            Debug.Log("Все ключи собраны. Можно идти к двери.");
    }

    private void StoreCollectedKeysForCurrentLevel()
    {
        if (!IsValidLevelIndex(currentLevelIndex))
            return;

        int maxKeys = Mathf.Max(0, CurrentLevel.keysOnLevel);
        collectedKeys = Mathf.Clamp(collectedKeys, 0, maxKeys);

        collectedKeysByLevel[currentLevelIndex] = collectedKeys;
    }

    public void TryUseDoor(int targetLevelIndex, bool requireAllKeys, Transform customSpawnPoint = null)
    {
        if (isTransitioning)
            return;

        if (targetLevelIndex < 0 || targetLevelIndex >= levels.Length)
        {
            Debug.LogWarning($"LevelManager: targetLevelIndex {targetLevelIndex} вне диапазона.");
            return;
        }

        if (CurrentLevel == null)
            return;

        if (requireAllKeys && collectedKeys < CurrentLevel.keysOnLevel)
        {
            Debug.Log($"Нужно собрать все ключи. Собрано: {collectedKeys}/{CurrentLevel.keysOnLevel}");
            return;
        }

        if (requireAllKeys)
            MarkLevelCompleted(currentLevelIndex);

        if (!IsLevelUnlocked(targetLevelIndex))
        {
            Debug.Log(
                $"Уровень \"{levels[targetLevelIndex].levelName}\" заблокирован. " +
                $"Нужно пройти: {GetRequiredLevelsNames(targetLevelIndex)}"
            );

            return;
        }

        StartCoroutine(TransitionToLevelRoutine(targetLevelIndex, customSpawnPoint));
    }

    private IEnumerator TransitionToLevelRoutine(int targetLevelIndex, Transform customSpawnPoint)
    {
        isTransitioning = true;

        yield return FadeImageAlpha(1f);

        currentLevelIndex = Mathf.Clamp(targetLevelIndex, 0, levels.Length - 1);

        ApplyLevelData(true, customSpawnPoint);

        if (blackScreenHoldDuration > 0f)
            yield return new WaitForSecondsRealtime(blackScreenHoldDuration);

        yield return FadeImageAlpha(0f);

        ShowCurrentLevelTitle();

        isTransitioning = false;
    }

    private void ApplyLevelData(bool movePlayerToSpawn, Transform customSpawnPoint = null)
    {
        LevelData level = CurrentLevel;

        if (level == null)
            return;

        if (movePlayerToSpawn)
        {
            Transform targetSpawn = customSpawnPoint != null ? customSpawnPoint : level.spawnPoint;

            if (targetSpawn != null)
            {
                MovePlayerToPosition(targetSpawn.position);
            }
            else
            {
                Debug.LogWarning(
                    $"LevelManager: у уровня \"{level.levelName}\" не назначен spawnPoint."
                );
            }
        }

        ApplyConfiner(level);
        ApplyObjectsState(level);
        SetupCurrentLevelUIOnly();
        ApplyAudioForCurrentLevel();
    }

    private void ShowCurrentLevelTitle()
    {
        if (CurrentLevel == null)
            return;

        if (levelTitleRoot != null)
            levelTitleRoot.SetActive(true);

        if (levelTitleTypewriter == null && levelTitleRoot != null)
            levelTitleTypewriter = levelTitleRoot.GetComponentInChildren<TypewriterTextUI>(true);

        if (levelTitleTypewriter == null)
        {
            Debug.LogWarning("LevelManager: не назначен levelTitleTypewriter.");
            return;
        }

        string levelName = string.IsNullOrEmpty(CurrentLevel.levelName)
            ? $"Уровень {currentLevelIndex + 1}"
            : CurrentLevel.levelName;

        string text = string.Format(levelTitleFormat, levelName);

        levelTitleTypewriter.PlayTextThenHide(text);
    }

    private void MovePlayerToPosition(Vector3 targetPosition)
    {
        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector2.zero;
            playerRb.angularVelocity = 0f;
            playerRb.position = targetPosition;
        }
        else
        {
            player.position = targetPosition;
        }
    }

    private void ApplyConfiner(LevelData level)
    {
        if (confiner2D == null)
            return;

        Collider2D targetConfiner = null;

        if (level != null && level.levelConfinerShape != null)
            targetConfiner = level.levelConfinerShape;
        else if (firstConfinerShape != null)
            targetConfiner = firstConfinerShape;

        if (targetConfiner == null)
        {
            Debug.LogWarning(
                $"LevelManager: для уровня \"{level.levelName}\" не назначен levelConfinerShape и не назначен firstConfinerShape."
            );

            return;
        }

        confiner2D.BoundingShape2D = targetConfiner;
        confiner2D.InvalidateBoundingShapeCache();
        confiner2D.InvalidateLensCache();
    }

    private void ApplyAudioForCurrentLevel()
    {
        if (AudioManager.Instance == null)
            return;

        LevelData level = CurrentLevel;

        if (level == null)
            return;

        AudioManager.Instance.PlayLevelAudio(
            level.backgroundMusicClips,
            level.randomAmbientClips,
            level.ambientRandomDelayRange,
            level.ambientRandomVolumeRange
        );
    }

    private void ApplyObjectsState(LevelData level)
    {
        SetObjectsActive(level.objectsToDisableOnEnter, false);
        SetObjectsActive(level.objectsToEnableOnEnter, true);
    }

    private void SetObjectsActive(GameObject[] objects, bool state)
    {
        if (objects == null)
            return;

        foreach (GameObject obj in objects)
        {
            if (obj == null)
                continue;

            obj.SetActive(state);
        }
    }

    private void SetupCurrentLevelUIOnly()
    {
        if (CurrentLevel == null)
            return;

        int maxKeys = Mathf.Max(0, CurrentLevel.keysOnLevel);

        if (IsLevelCompleted(currentLevelIndex))
            collectedKeys = maxKeys;
        else
            collectedKeys = GetCollectedKeysFromMemory(currentLevelIndex);

        RebuildKeyUI();
        UpdateKeyUI();
    }

    private int GetCollectedKeysFromMemory(int levelIndex)
    {
        if (!IsValidLevelIndex(levelIndex))
            return 0;

        int maxKeys = Mathf.Max(0, levels[levelIndex].keysOnLevel);
        return Mathf.Clamp(collectedKeysByLevel[levelIndex], 0, maxKeys);
    }

    private void RebuildKeyUI()
    {
        if (CurrentLevel == null)
            return;

        if (keyBarRoot == null || keyIconPrefab == null)
        {
            Debug.LogWarning("LevelManager: не назначены keyBarRoot или keyIconPrefab.");
            return;
        }

        RectTransform rootRect = keyBarRoot.GetComponent<RectTransform>();

        if (rootRect == null)
        {
            Debug.LogError("LevelManager: keyBarRoot должен быть UI-объектом внутри Canvas с RectTransform.");
            return;
        }

        for (int i = rootRect.childCount - 1; i >= 0; i--)
        {
            Destroy(rootRect.GetChild(i).gameObject);
        }

        spawnedKeyIcons.Clear();

        int keysCount = Mathf.Max(0, CurrentLevel.keysOnLevel);

        for (int i = 0; i < keysCount; i++)
        {
            Image icon = Instantiate(keyIconPrefab, rootRect);
            icon.gameObject.SetActive(true);

            RectTransform iconRect = icon.rectTransform;
            iconRect.anchoredPosition = new Vector2(i * keySpacing, 0f);
            iconRect.localScale = Vector3.one;

            spawnedKeyIcons.Add(icon);
        }
    }

    private void UpdateKeyUI()
    {
        if (spawnedKeyIcons == null || spawnedKeyIcons.Count == 0)
            return;

        for (int i = 0; i < spawnedKeyIcons.Count; i++)
        {
            if (spawnedKeyIcons[i] == null)
                continue;

            spawnedKeyIcons[i].color = i < collectedKeys
                ? collectedKeyColor
                : defaultKeyColor;
        }
    }

    private IEnumerator FadeImageAlpha(float targetAlpha)
    {
        if (fadeImage == null)
            yield break;

        fadeImage.gameObject.SetActive(true);

        Color color = fadeImage.color;
        float startAlpha = color.a;
        float elapsed = 0f;

        if (fadeDuration <= 0f)
        {
            color.a = targetAlpha;
            fadeImage.color = color;
            yield break;
        }

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);

            color.a = Mathf.Lerp(startAlpha, targetAlpha, t);
            fadeImage.color = color;

            yield return null;
        }

        color.a = targetAlpha;
        fadeImage.color = color;
    }

    public void MarkLevelCompleted(int levelIndex)
    {
        if (!IsValidLevelIndex(levelIndex))
            return;

        completedLevelStates[levelIndex] = true;

        int maxKeys = Mathf.Max(0, levels[levelIndex].keysOnLevel);
        collectedKeysByLevel[levelIndex] = maxKeys;

        if (levelIndex == currentLevelIndex)
            collectedKeys = maxKeys;

        Debug.Log($"Уровень пройден: {levels[levelIndex].levelName}");
    }

    public bool IsLevelCompleted(int levelIndex)
    {
        if (!IsValidLevelIndex(levelIndex))
            return false;

        return completedLevelStates[levelIndex];
    }

    public bool IsLevelUnlocked(int levelIndex)
    {
        if (!IsValidLevelIndex(levelIndex))
            return false;

        LevelData targetLevel = levels[levelIndex];

        if (!targetLevel.lockedUntilRequiredLevelsCompleted)
            return true;

        if (targetLevel.requiredCompletedLevelIndices == null ||
            targetLevel.requiredCompletedLevelIndices.Length == 0)
            return true;

        for (int i = 0; i < targetLevel.requiredCompletedLevelIndices.Length; i++)
        {
            int requiredIndex = targetLevel.requiredCompletedLevelIndices[i];

            if (!IsValidLevelIndex(requiredIndex))
            {
                Debug.LogWarning(
                    $"LevelManager: у уровня \"{targetLevel.levelName}\" " +
                    $"указан некорректный requiredCompletedLevelIndex = {requiredIndex}"
                );

                return false;
            }

            if (!IsLevelCompleted(requiredIndex))
                return false;
        }

        return true;
    }

    public string GetRequiredLevelsNames(int levelIndex)
    {
        if (!IsValidLevelIndex(levelIndex))
            return string.Empty;

        LevelData targetLevel = levels[levelIndex];

        if (targetLevel.requiredCompletedLevelIndices == null ||
            targetLevel.requiredCompletedLevelIndices.Length == 0)
            return string.Empty;

        List<string> names = new List<string>();

        for (int i = 0; i < targetLevel.requiredCompletedLevelIndices.Length; i++)
        {
            int requiredIndex = targetLevel.requiredCompletedLevelIndices[i];

            if (IsValidLevelIndex(requiredIndex))
                names.Add(levels[requiredIndex].levelName);
            else
                names.Add($"Index {requiredIndex}");
        }

        return string.Join(", ", names);
    }

    public int GetCollectedKeys()
    {
        return collectedKeys;
    }

    public int GetCollectedKeysForLevel(int levelIndex)
    {
        return GetCollectedKeysFromMemory(levelIndex);
    }

    public int GetRequiredKeysForCurrentLevel()
    {
        return CurrentLevel != null ? CurrentLevel.keysOnLevel : 0;
    }

    public string GetCurrentLevelName()
    {
        return CurrentLevel != null ? CurrentLevel.levelName : string.Empty;
    }

    public void SetCurrentLevelFromCode(int levelIndex, bool movePlayerToSpawn = true)
    {
        if (levels == null || levels.Length == 0)
            return;

        currentLevelIndex = Mathf.Clamp(levelIndex, 0, levels.Length - 1);

        ApplyLevelData(movePlayerToSpawn);
        ShowCurrentLevelTitle();
    }

    // Оставлено для совместимости. Теперь ничего не сохраняет.
    public void SaveCurrentProgressNow()
    {
        StoreCollectedKeysForCurrentLevel();
    }

    [ContextMenu("Apply Current Level From Inspector")]
    public void ApplyCurrentLevelFromInspector()
    {
        if (levels == null || levels.Length == 0)
            return;

        currentLevelIndex = Mathf.Clamp(currentLevelIndex, 0, levels.Length - 1);

        ApplyLevelData(true);
        ShowCurrentLevelTitle();
    }

    [ContextMenu("Reset Runtime Level Progress")]
    public void ResetAllLevelProgress()
    {
        if (levels == null)
            return;

        completedLevelStates = new bool[levels.Length];
        collectedKeysByLevel = new int[levels.Length];

        for (int i = 0; i < levels.Length; i++)
        {
            completedLevelStates[i] = false;
            collectedKeysByLevel[i] = 0;
        }

        currentLevelIndex = Mathf.Clamp(currentLevelIndex, 0, levels.Length - 1);
        collectedKeys = 0;

        ApplyLevelData(true);
        ShowCurrentLevelTitle();
    }

    private bool IsValidLevelIndex(int levelIndex)
    {
        return levels != null &&
               levelIndex >= 0 &&
               levelIndex < levels.Length &&
               completedLevelStates != null &&
               collectedKeysByLevel != null &&
               levelIndex < completedLevelStates.Length &&
               levelIndex < collectedKeysByLevel.Length;
    }
}
