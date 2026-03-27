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

        if (requireAllKeys && collectedKeys < CurrentLevel.keysOnLevel)
        {
            Debug.Log("Нужно собрать все ключи.");
            return;
        }

        if (targetLevelIndex < 0 || targetLevelIndex >= levels.Length)
        {
            Debug.LogWarning($"LevelManager: targetLevelIndex {targetLevelIndex} вне диапазона.");
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

        for (int i = 0; i < CurrentLevel.keysOnLevel; i++)
        {
            Image icon = Instantiate(keyIconPrefab, rootRect);
            RectTransform rect = icon.rectTransform;

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(-i * keySpacing, 0f);

            icon.color = defaultKeyColor;
            spawnedKeyIcons.Add(icon);
        }
    }

    private void UpdateKeyUI()
    {
        for (int i = 0; i < spawnedKeyIcons.Count; i++)
        {
            if (spawnedKeyIcons[i] == null) continue;
            spawnedKeyIcons[i].color = i < collectedKeys ? collectedKeyColor : defaultKeyColor;
        }
    }

    private IEnumerator FadeImageAlpha(float targetAlpha)
    {
        if (fadeImage == null)
            yield break;

        Color color = fadeImage.color;
        float startAlpha = color.a;
        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / fadeDuration);

            color.a = Mathf.Lerp(startAlpha, targetAlpha, t);
            fadeImage.color = color;

            yield return null;
        }

        color.a = targetAlpha;
        fadeImage.color = color;
    }
}