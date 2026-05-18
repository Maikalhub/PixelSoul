using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuSceneFadeLoader : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string menuSceneName = "Menu";

    [Header("Fade Image")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 0.25f;
    [SerializeField] private float targetAlpha = 1f;

    private bool isLoading;

    private void Awake()
    {
        InitializeFade();
    }

    private void InitializeFade()
    {
        if (fadeImage == null)
            return;

        fadeImage.gameObject.SetActive(true);

        Color color = fadeImage.color;
        color.a = 0f;
        fadeImage.color = color;

        fadeImage.raycastTarget = false;
    }

    public void LoadMenu()
    {
        if (isLoading)
            return;

        if (string.IsNullOrWhiteSpace(menuSceneName))
        {
            Debug.LogWarning("MenuSceneFadeLoader: название сцены меню не указано.");
            return;
        }

        StartCoroutine(FadeAndLoadRoutine(menuSceneName));
    }

    public void LoadSceneByInspectorName()
    {
        LoadMenu();
    }

    private IEnumerator FadeAndLoadRoutine(string sceneName)
    {
        isLoading = true;

        if (fadeImage != null)
            fadeImage.raycastTarget = true;

        yield return FadeImageToAlpha(targetAlpha, fadeDuration);

        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator FadeImageToAlpha(float alpha, float duration)
    {
        if (fadeImage == null)
            yield break;

        float elapsed = 0f;

        Color color = fadeImage.color;
        float startAlpha = color.a;

        if (duration <= 0f)
        {
            color.a = alpha;
            fadeImage.color = color;
            yield break;
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            color.a = Mathf.Lerp(startAlpha, alpha, t);

            fadeImage.color = color;

            yield return null;
        }

        color.a = alpha;
        fadeImage.color = color;
    }
}