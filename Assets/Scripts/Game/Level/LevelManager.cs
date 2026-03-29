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
        [Tooltip("Массив фоновых треков для этого уровня")]
        public AudioClip[] backgroundMusicClips;

        [Tooltip("Случайные ambient-звуки: скрип, ветер, капли, стук и т.д.")]
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

    [Header("Стартовые настройки")]
    [SerializeField] private int startLevelIndex = 0;
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

    [Header("Fade")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 0.35f;

    private readonly List<Image> spawnedKeyIcons = new();
    private readonly HashSet<int> completedLevels = new();

    private int currentLevelIndex;
    private int collectedKeys;
    private bool isTransitioning;

    private LevelData CurrentLevel => levels[currentLevelIndex];

    public bool HasAllKeys => collectedKeys >= CurrentLevel.keysOnLevel;
    public bool IsTransitioning => isTransitioning;
    public int CurrentLevelIndex => currentLevelIndex;

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

        currentLevelIndex = Mathf.Clamp(startLevelIndex, 0, levels.Length - 1);
        collectedKeys = 0;

        ApplyFirstConfiner();
        ApplyLevelData(false);

        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
            fadeImage.gameObject.SetActive(true);
        }
    }

    public void CollectKey(GameObject keyObject)
    {
        if (isTransitioning) return;
        if (collectedKeys >= CurrentLevel.keysOnLevel) return;

        collectedKeys++;
        UpdateKeyUI();

        if (keyObject != null)
            Destroy(keyObject);

        if (collectedKeys >= CurrentLevel.keysOnLevel)
            Debug.Log("Все ключи собраны. Можно идти к двери.");
    }

    public void TryUseDoor(int targetLevelIndex, bool requireAllKeys, Transform customSpawnPoint = null)
    {
        if (isTransitioning) return;

        if (targetLevelIndex < 0 || targetLevelIndex >= levels.Length)
        {
            Debug.LogWarning($"LevelManager: targetLevelIndex {targetLevelIndex} вне диапазона.");
            return;
        }

        if (requireAllKeys && collectedKeys < CurrentLevel.keysOnLevel)
        {
            Debug.Log("Нужно собрать все ключи.");
            return;
        }

        // Если дверь требует все ключи, считаем текущий уровень пройденным
        if (requireAllKeys)
        {
            MarkLevelCompleted(currentLevelIndex);
        }

        // Проверяем, открыт ли целевой уровень
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

        currentLevelIndex = targetLevelIndex;
        ApplyLevelData(true, customSpawnPoint);

        yield return null;

        yield return FadeImageAlpha(0f);

        isTransitioning = false;
    }

    private void ApplyFirstConfiner()
    {
        if (confiner2D == null || firstConfinerShape == null)
            return;

        confiner2D.BoundingShape2D = firstConfinerShape;
        confiner2D.InvalidateBoundingShapeCache();
        confiner2D.InvalidateLensCache();
    }

    private void ApplyLevelData(bool movePlayerToSpawn, Transform customSpawnPoint = null)
    {
        LevelData level = CurrentLevel;

        if (movePlayerToSpawn)
        {
            if (customSpawnPoint != null)
                player.position = customSpawnPoint.position;
            else if (level.spawnPoint != null)
                player.position = level.spawnPoint.position;
        }

        if (confiner2D != null && level.levelConfinerShape != null)
        {
            confiner2D.BoundingShape2D = level.levelConfinerShape;
            confiner2D.InvalidateBoundingShapeCache();
            confiner2D.InvalidateLensCache();
        }

        ApplyObjectsState(level);
        SetupCurrentLevelUIOnly();
        ApplyAudioForCurrentLevel();
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
        if (objects == null) return;

        foreach (GameObject obj in objects)
        {
            if (obj == null) continue;
            obj.SetActive(state);
        }
    }

    private void SetupCurrentLevelUIOnly()
    {
        collectedKeys = 0;
        RebuildKeyUI();
        UpdateKeyUI();
    }

    private void RebuildKeyUI()
    {
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
            if (spawnedKeyIcons[i] == null) continue;

            spawnedKeyIcons[i].color = i < collectedKeys
                ? collectedKeyColor
                : defaultKeyColor;
        }
    }

    private IEnumerator FadeImageAlpha(float targetAlpha)
    {
        if (fadeImage == null)
            yield break;

        Color color = fadeImage.color;
        float startAlpha = color.a;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = fadeDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / fadeDuration);

            color.a = Mathf.Lerp(startAlpha, targetAlpha, t);
            fadeImage.color = color;

            yield return null;
        }

        color.a = targetAlpha;
        fadeImage.color = color;
    }

    public void MarkLevelCompleted(int levelIndex)
    {
        if (levelIndex < 0 || levelIndex >= levels.Length)
            return;

        if (completedLevels.Add(levelIndex))
        {
            Debug.Log($"Уровень пройден: {levels[levelIndex].levelName}");
        }
    }

    public bool IsLevelCompleted(int levelIndex)
    {
        return completedLevels.Contains(levelIndex);
    }

    public bool IsLevelUnlocked(int levelIndex)
    {
        if (levelIndex < 0 || levelIndex >= levels.Length)
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

            if (requiredIndex < 0 || requiredIndex >= levels.Length)
            {
                Debug.LogWarning(
                    $"LevelManager: у уровня \"{targetLevel.levelName}\" " +
                    $"указан некорректный requiredCompletedLevelIndex = {requiredIndex}"
                );
                return false;
            }

            if (!completedLevels.Contains(requiredIndex))
                return false;
        }

        return true;
    }

    public string GetRequiredLevelsNames(int levelIndex)
    {
        if (levelIndex < 0 || levelIndex >= levels.Length)
            return string.Empty;

        LevelData targetLevel = levels[levelIndex];

        if (targetLevel.requiredCompletedLevelIndices == null ||
            targetLevel.requiredCompletedLevelIndices.Length == 0)
            return string.Empty;

        List<string> names = new List<string>();

        for (int i = 0; i < targetLevel.requiredCompletedLevelIndices.Length; i++)
        {
            int requiredIndex = targetLevel.requiredCompletedLevelIndices[i];

            if (requiredIndex >= 0 && requiredIndex < levels.Length)
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

    public int GetRequiredKeysForCurrentLevel()
    {
        return CurrentLevel != null ? CurrentLevel.keysOnLevel : 0;
    }

    public string GetCurrentLevelName()
    {
        return CurrentLevel != null ? CurrentLevel.levelName : string.Empty;
    }
}