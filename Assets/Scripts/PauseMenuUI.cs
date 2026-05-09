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
    [SerializeField] private Button exitButton;

    [Header("Optional Settings Panel")]
    [SerializeField] private GameObject settingsPanel;

    [Header("Input")]
    [SerializeField] private bool useEscapeKey = true;
    [SerializeField] private Key pauseKey = Key.Escape;

    [Header("Exit")]
    [SerializeField] private bool exitToMainMenu = false;
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private bool isPaused;

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

        if (exitButton != null)
            exitButton.onClick.AddListener(ExitGame);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        ResumeGame();
    }

    private void Update()
    {
        if (!useEscapeKey)
            return;

        if (Keyboard.current == null)
            return;

        if (Keyboard.current[pauseKey].wasPressedThisFrame)
            TogglePause();
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

            if (exitButton == null &&
                (buttonName.Contains("exit") || buttonName.Contains("quit") || buttonName.Contains("выход")))
            {
                exitButton = button;
                continue;
            }
        }
    }

    public void TogglePause()
    {
        if (isPaused)
            ResumeGame();
        else
            PauseGame();
    }

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;

        ShowMenu(true);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        ShowMenu(false);
    }

    public void OpenSettings()
    {
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

    public void ExitGame()
    {
        Time.timeScale = 1f;

        if (exitToMainMenu)
        {
            SceneManager.LoadScene(mainMenuSceneName);
            return;
        }

        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
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