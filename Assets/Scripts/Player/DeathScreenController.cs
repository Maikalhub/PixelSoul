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

    private void Awake()
    {
        Initialize();
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

        // Важно: после затемнения не блокируем кнопки
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

    public void OnRestartButton()
    {
        StartCoroutine(FadeAndLoad(SceneManager.GetActiveScene().name));
    }

    public void OnMainMenuButton()
    {
        StartCoroutine(FadeAndLoad(menuSceneName));
    }

    private IEnumerator FadeAndLoad(string sceneName)
    {
        if (fadeImage != null)
            fadeImage.raycastTarget = true;

        yield return StartCoroutine(FadeImageToAlpha(1f, 0.25f));
        SceneManager.LoadScene(sceneName);
    }
}