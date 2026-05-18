using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class PauseMenuUI : MonoBehaviour
{
    [Header("Menu")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Buttons")]
    [SerializeField] private Button continueButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button quitButton;

    [Header("Optional Settings Panel")]
    [SerializeField] private GameObject settingsPanel;

    [Header("Input")]
    [SerializeField] private bool useEscapeKey = true;
    [SerializeField] private Key pauseKey = Key.Escape;

    [Header("Main Menu Scene")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Fade To Menu")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeToMenuDuration = 0.25f;
    [SerializeField] private float fadeTargetAlpha = 1f;

    private bool isPaused;
    private bool isLoadingScene;

    public bool IsPaused => isPaused;

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        AutoFindButtons();

        if (continueButton != null)
            continueButton.onClick.AddListener(ResumeGame);

        if (settingsButton != null)
            settingsButton.onClick.AddListener(OpenSettings);

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(OnMainMenuButton);

        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitButton);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        InitializeFadeImage();

        ResumeGame();
    }

    private void Update()
    {
        if (isLoadingScene)
            return;

        if (!useEscapeKey)
            return;

        if (Keyboard.current == null)
            return;

        if (Keyboard.current[pauseKey].wasPressedThisFrame)
        {
            // Если открыты настройки — сначала закрываем их
            if (IsSettingsOpen())
            {
                CloseSettings();
            }
            else
            {
                // Иначе стандартное переключение паузы
                TogglePause();
            }
        }
    }

    private void InitializeFadeImage()
    {
        if (fadeImage == null)
            return;

        fadeImage.gameObject.SetActive(true);

        Color color = fadeImage.color;
        color.a = 0f;
        fadeImage.color = color;

        fadeImage.raycastTarget = false;
    }

    private void AutoFindButtons()
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);

        foreach (Button button in buttons)
        {
            string buttonName = button.gameObject.name.ToLower();

            if (continueButton == null &&
                (buttonName.Contains("continue") || buttonName.Contains("resume") || buttonName.Contains("продолж")))
            {
                continueButton = button;
                continue;
            }

            if (settingsButton == null &&
                (buttonName.Contains("settings") || buttonName.Contains("setting") || buttonName.Contains("настрой")))
            {
                settingsButton = button;
                continue;
            }

            if (mainMenuButton == null &&
                (buttonName.Contains("menu") || buttonName.Contains("mainmenu") || buttonName.Contains("меню")))
            {
                mainMenuButton = button;
                continue;
            }

            if (quitButton == null &&
                (buttonName.Contains("quit") || buttonName.Contains("exit") || buttonName.Contains("выход")))
            {
                quitButton = button;
                continue;
            }
        }
    }

    public void TogglePause()
    {
        if (isLoadingScene)
            return;

        if (isPaused)
            ResumeGame();
        else
            PauseGame();
    }

    public void PauseGame()
    {
        if (isLoadingScene)
            return;

        isPaused = true;
        Time.timeScale = 0f;

        ShowMenu(true);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    public void ResumeGame()
    {
        if (isLoadingScene)
            return;

        isPaused = false;
        Time.timeScale = 1f;

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        ShowMenu(false);
    }

    public void OpenSettings()
    {
        if (isLoadingScene)
            return;

        if (settingsPanel == null)
        {
            Debug.LogWarning("PauseMenuUI: Settings Panel не назначен.");
            return;
        }

        settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    /// <summary>
    /// Проверяет, открыта ли панель настроек
    /// </summary>
    private bool IsSettingsOpen()
    {
        return settingsPanel != null && settingsPanel.activeSelf;
    }

    public void OnMainMenuButton()
    {
        if (isLoadingScene)
            return;

        StartCoroutine(FadeAndLoadMainMenu());
    }

    public void OnQuitButton()
    {
        if (isLoadingScene)
            return;

        Time.timeScale = 1f;

        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private IEnumerator FadeAndLoadMainMenu()
    {
        isLoadingScene = true;

        if (continueButton != null)
            continueButton.interactable = false;

        if (settingsButton != null)
            settingsButton.interactable = false;

        if (mainMenuButton != null)
            mainMenuButton.interactable = false;

        if (quitButton != null)
            quitButton.interactable = false;

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (fadeImage != null)
            fadeImage.raycastTarget = true;

        yield return StartCoroutine(FadeImageToAlpha(fadeTargetAlpha, fadeToMenuDuration));

        Time.timeScale = 1f;

        if (string.IsNullOrWhiteSpace(mainMenuSceneName))
        {
            Debug.LogWarning("PauseMenuUI: Main Menu Scene Name не указан.");
            isLoadingScene = false;
            yield break;
        }

        SceneManager.LoadScene(mainMenuSceneName);
    }

    private IEnumerator FadeImageToAlpha(float targetAlpha, float duration)
    {
        if (fadeImage == null)
            yield break;

        fadeImage.gameObject.SetActive(true);

        Color color = fadeImage.color;
        float startAlpha = color.a;
        float elapsed = 0f;

        if (duration <= 0f)
        {
            color.a = targetAlpha;
            fadeImage.color = color;
            yield break;
        }

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            color.a = Mathf.Lerp(startAlpha, targetAlpha, t);

            fadeImage.color = color;

            yield return null;
        }

        color.a = targetAlpha;
        fadeImage.color = color;
    }

    private void ShowMenu(bool show)
    {
        if (canvasGroup == null)
            return;

        canvasGroup.alpha = show ? 1f : 0f;
        canvasGroup.interactable = show;
        canvasGroup.blocksRaycasts = show;
    }
}