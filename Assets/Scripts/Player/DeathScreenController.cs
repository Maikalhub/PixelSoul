using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DeathScreenController : MonoBehaviour
{
    [Header("Fade Image")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private float startDelay = 0.4f;
    [SerializeField] private float screenFadeDuration = 1f;
    [SerializeField] private float screenTargetAlpha = 1f;

    [Header("UI Fade")]
    [SerializeField] private CanvasGroup deathTextGroup;
    [SerializeField] private CanvasGroup restartButtonGroup;
    [SerializeField] private CanvasGroup mainMenuButtonGroup;
    [SerializeField] private float textFadeDuration = 0.4f;
    [SerializeField] private float buttonFadeDuration = 0.35f;
    [SerializeField] private float betweenElementsDelay = 0.12f;

    [Header("Scene Names")]
    [SerializeField] private string menuSceneName = "Menu";

    private bool isShowing;

    // Ключ для временного хранения индекса уровня между перезагрузками сцены
    private const string RESTART_LEVEL_INDEX_KEY = "TempRestartLevelIndex";

    private void Awake()
    {
        Initialize();

        // Восстанавливаем уровень после перезагрузки (если был рестарт)
        RestoreLevelAfterRestart();
    }

    private void Initialize()
    {
        if (fadeImage != null)
        {
            Color color = fadeImage.color;
            color.a = 0f;
            fadeImage.color = color;
            fadeImage.raycastTarget = false;
            fadeImage.gameObject.SetActive(true);
        }

        PrepareCanvasGroup(deathTextGroup);
        PrepareCanvasGroup(restartButtonGroup);
        PrepareCanvasGroup(mainMenuButtonGroup);
    }

    private void PrepareCanvasGroup(CanvasGroup group)
    {
        if (group == null) return;

        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
        group.gameObject.SetActive(true);
    }

    public void ShowDeathScreen()
    {
        if (isShowing) return;
        StartCoroutine(ShowDeathScreenRoutine());
    }

    private IEnumerator ShowDeathScreenRoutine()
    {
        isShowing = true;

        if (startDelay > 0f)
            yield return new WaitForSeconds(startDelay);

        yield return StartCoroutine(FadeImageToAlpha(screenTargetAlpha, screenFadeDuration));

        if (fadeImage != null)
            fadeImage.raycastTarget = false;

        if (deathTextGroup != null)
            yield return StartCoroutine(FadeCanvasGroup(deathTextGroup, 1f, textFadeDuration, false));

        if (betweenElementsDelay > 0f)
            yield return new WaitForSeconds(betweenElementsDelay);

        if (restartButtonGroup != null)
            yield return StartCoroutine(FadeCanvasGroup(restartButtonGroup, 1f, buttonFadeDuration, true));

        if (betweenElementsDelay > 0f)
            yield return new WaitForSeconds(betweenElementsDelay);

        if (mainMenuButtonGroup != null)
            yield return StartCoroutine(FadeCanvasGroup(mainMenuButtonGroup, 1f, buttonFadeDuration, true));
    }

    private IEnumerator FadeImageToAlpha(float targetAlpha, float duration)
    {
        if (fadeImage == null) yield break;

        float elapsed = 0f;
        Color color = fadeImage.color;
        float startAlpha = color.a;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            fadeImage.color = color;
            yield return null;
        }

        color.a = targetAlpha;
        fadeImage.color = color;
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup group, float targetAlpha, float duration, bool enableInteractionOnComplete)
    {
        if (group == null) yield break;

        float elapsed = 0f;
        float startAlpha = group.alpha;

        group.gameObject.SetActive(true);
        group.interactable = false;
        group.blocksRaycasts = false;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            group.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            yield return null;
        }

        group.alpha = targetAlpha;

        if (enableInteractionOnComplete)
        {
            group.interactable = true;
            group.blocksRaycasts = true;
        }
    }

    // ================= КНОПКИ =================

    /// <summary>
    /// Рестарт текущего уровня
    /// </summary>
    public void OnRestartButton()
    {
        // Сохраняем индекс текущего уровня перед перезагрузкой сцены
        StoreCurrentLevelIndexForRestart();

        StartCoroutine(FadeAndLoad(SceneManager.GetActiveScene().name));
    }

    /// <summary>
    /// Выход в главное меню
    /// </summary>
    public void OnMainMenuButton()
    {
        // Очищаем временный индекс, т.к. выходим в меню
        PlayerPrefs.DeleteKey(RESTART_LEVEL_INDEX_KEY);

        StartCoroutine(FadeAndLoad(menuSceneName));
    }

    private IEnumerator FadeAndLoad(string sceneName)
    {
        if (fadeImage != null)
            fadeImage.raycastTarget = true;

        yield return StartCoroutine(FadeImageToAlpha(1f, 0.25f));
        SceneManager.LoadScene(sceneName);
    }

    // ================= СОХРАНЕНИЕ / ВОССТАНОВЛЕНИЕ УРОВНЯ =================

    /// <summary>
    /// Сохраняет индекс текущего уровня в PlayerPrefs перед рестартом
    /// </summary>
    private void StoreCurrentLevelIndexForRestart()
    {
        if (LevelManager.Instance != null)
        {
            int currentIndex = LevelManager.Instance.CurrentLevelIndex;
            PlayerPrefs.SetInt(RESTART_LEVEL_INDEX_KEY, currentIndex);
            PlayerPrefs.Save();

            Debug.Log($"DeathScreenController: Сохранён индекс уровня {currentIndex} для рестарта.");
        }
    }

    /// <summary>
    /// Восстанавливает уровень после перезагрузки сцены (если был рестарт)
    /// </summary>
    private void RestoreLevelAfterRestart()
    {
        if (PlayerPrefs.HasKey(RESTART_LEVEL_INDEX_KEY))
        {
            int savedIndex = PlayerPrefs.GetInt(RESTART_LEVEL_INDEX_KEY);

            // Очищаем ключ, чтобы не восстановить случайно при следующей загрузке
            PlayerPrefs.DeleteKey(RESTART_LEVEL_INDEX_KEY);

            // Восстанавливаем уровень через LevelManager
            if (LevelManager.Instance != null)
            {
                Debug.Log($"DeathScreenController: Восстановление уровня {savedIndex} после рестарта.");
                LevelManager.Instance.SetCurrentLevelFromCode(savedIndex, true);
            }
        }
    }
}