using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    private enum MenuState { ScreenMenu, MainMenu }
    private MenuState currentState;

    [Header("INFO")]
    [TextArea(3, 6)]
    [SerializeField]
    private string info =
        "Этот скрипт управляет главным меню.\n" +
        "• Любая кнопка → переход в MainMenu\n" +
        "• Бездействие → возврат в ScreenMenu\n" +
        "• Есть плавное затемнение сцены";

    [Header("ROOT MENUS")]
    [SerializeField] private GameObject screenMenu;
    [SerializeField] private GameObject mainMenu;

    [Header("SUB MENUS")]
    [SerializeField] private GameObject startMenu;
    [SerializeField] private GameObject newgameMenu;
    [SerializeField] private GameObject loadMenu;
    [SerializeField] private GameObject levelMenu;
    [SerializeField] private GameObject settingsMenu;

    [Header("SETTINGS MENU TABS")]
    [SerializeField] private GameObject[] settingsTabs;
    [SerializeField] private int defaultSettingsTab = 0;

    [Header("UI")]
    [SerializeField] private BlinkText screenMenuBlinkText;

    [Header("Audio")]
    [SerializeField] private AudioSource uiAudioSource;
    [SerializeField] private AudioSource musicAudioSource;
    [SerializeField] private AudioClip transitionSound;
    [SerializeField] private AudioClip[] backgroundMusic;
    [SerializeField] private float musicFadeDuration = 1f;

    [Header("Fade / Transition")]
    [SerializeField] private Image backgroundFade;
    [SerializeField] private float fadeDuration = 0.5f;

    [Header("Idle Return")]
    [SerializeField] private float idleReturnTime = 15f;

    [Header("Scene Load")]
    [SerializeField] private string sceneToLoad;

    [Header("EXIT SETTINGS")]
    [SerializeField] private GameObject exitTextObject;
    [SerializeField] private float exitFadeDuration = 1f;

    private float idleTimer;
    private bool isTransitioning;
    private int currentMusicIndex = 0;

    // ================= UNITY =================
    private void Start()
    {
        currentState = MenuState.ScreenMenu;
        ShowScreenMenuImmediate();

        if (backgroundFade != null)
        {
            backgroundFade.gameObject.SetActive(true);
            backgroundFade.raycastTarget = false;
            SetFadeAlpha(0f);
        }

        if (exitTextObject != null)
            exitTextObject.SetActive(false);

        PlayBackgroundMusic(currentMusicIndex);
    }

    private void Update()
    {
        if (currentState == MenuState.ScreenMenu && !isTransitioning)
        {
            if (Input.anyKeyDown || Input.GetMouseButtonDown(0))
                OpenMainMenu(null);

            return;
        }

        if (currentState == MenuState.MainMenu && !isTransitioning)
        {
            idleTimer += Time.deltaTime;

            if (Input.anyKeyDown || Input.GetMouseButtonDown(0))
                ResetIdleTimer();

            if (idleTimer >= idleReturnTime)
                ReturnToScreenMenu();
        }
    }

    private void ResetIdleTimer()
    {
        idleTimer = 0f;
    }

    // ================= SCREEN MENU =================
    private void ShowScreenMenuImmediate()
    {
        if (screenMenu != null) screenMenu.SetActive(true);
        if (mainMenu != null) mainMenu.SetActive(false);

        screenMenuBlinkText?.ResetBlink();
        ResetIdleTimer();

        isTransitioning = false;
        currentState = MenuState.ScreenMenu;
    }

    private void ReturnToScreenMenu()
    {
        if (isTransitioning || currentState == MenuState.ScreenMenu) return;
        StartCoroutine(ReturnToScreenMenuRoutine());
    }

    private IEnumerator ReturnToScreenMenuRoutine()
    {
        isTransitioning = true;

        PlayTransitionSound();
        yield return StartCoroutine(FadeImage(0f, 1f));

        if (mainMenu != null) mainMenu.SetActive(false);
        if (screenMenu != null) screenMenu.SetActive(true);

        screenMenuBlinkText?.ResetBlink();

        yield return StartCoroutine(FadeImage(1f, 0f));

        currentState = MenuState.ScreenMenu;
        ResetIdleTimer();
        isTransitioning = false;
    }

    // ================= MAIN MENU =================
    public void OpenMainMenu(AudioClip clickSound)
    {
        if (isTransitioning || currentState != MenuState.ScreenMenu) return;

        isTransitioning = true;
        currentState = MenuState.MainMenu;

        PlayClickSound(clickSound);
        screenMenuBlinkText?.SetFastColorSpeed();

        StartCoroutine(OpenMainMenuRoutine());
    }

    private IEnumerator OpenMainMenuRoutine()
    {
        PlayTransitionSound();
        yield return StartCoroutine(FadeImage(0f, 1f));

        if (screenMenu != null) screenMenu.SetActive(false);
        if (mainMenu != null) mainMenu.SetActive(true);

        ShowStartMenu(null);

        yield return StartCoroutine(FadeImage(1f, 0f));

        ResetIdleTimer();
        isTransitioning = false;
    }

    // ================= SUB MENUS =================
    public void ShowStartMenu(AudioClip clickSound)
    {
        PlayClickSound(clickSound);
        ResetIdleTimer();
        DisableAllSubMenus();
        if (startMenu != null) startMenu.SetActive(true);
    }

    public void ShowNewGameMenu(AudioClip clickSound)
    {
        PlayClickSound(clickSound);
        ResetIdleTimer();
        DisableAllSubMenus();
        if (newgameMenu != null) newgameMenu.SetActive(true);
    }

    public void ShowLoadMenu(AudioClip clickSound)
    {
        PlayClickSound(clickSound);
        ResetIdleTimer();
        DisableAllSubMenus();
        if (loadMenu != null) loadMenu.SetActive(true);
    }

    public void ShowLevelMenu(AudioClip clickSound)
    {
        PlayClickSound(clickSound);
        ResetIdleTimer();
        DisableAllSubMenus();
        if (levelMenu != null) levelMenu.SetActive(true);
    }

    public void ShowSettingsMenu(AudioClip clickSound)
    {
        PlayClickSound(clickSound);
        ResetIdleTimer();
        DisableAllSubMenus();

        if (settingsMenu != null)
            settingsMenu.SetActive(true);

        ShowSettingsTabInternal(defaultSettingsTab);
    }

    private void DisableAllSubMenus()
    {
        if (startMenu != null) startMenu.SetActive(false);
        if (newgameMenu != null) newgameMenu.SetActive(false);
        if (loadMenu != null) loadMenu.SetActive(false);
        if (levelMenu != null) levelMenu.SetActive(false);
        if (settingsMenu != null) settingsMenu.SetActive(false);
    }

    // ================= SETTINGS TABS =================
    private void ShowSettingsTabInternal(int index)
    {
        if (settingsTabs == null || settingsTabs.Length == 0) return;
        if (index < 0 || index >= settingsTabs.Length) return;

        for (int i = 0; i < settingsTabs.Length; i++)
        {
            if (settingsTabs[i] != null)
                settingsTabs[i].SetActive(i == index);
        }
    }

    public void SettingsTab1(AudioClip clickSound)
    {
        PlayClickSound(clickSound);
        ResetIdleTimer();
        ShowSettingsTabInternal(0);
    }

    public void SettingsTab2(AudioClip clickSound)
    {
        PlayClickSound(clickSound);
        ResetIdleTimer();
        ShowSettingsTabInternal(1);
    }

    public void SettingsTab3(AudioClip clickSound)
    {
        PlayClickSound(clickSound);
        ResetIdleTimer();
        ShowSettingsTabInternal(2);
    }

    public void SettingsTab4(AudioClip clickSound)
    {
        PlayClickSound(clickSound);
        ResetIdleTimer();
        ShowSettingsTabInternal(3);
    }

    // ================= EXIT BUTTON =================
    public void ExitGame(AudioClip clickSound)
    {
        if (isTransitioning) return;

        PlayClickSound(clickSound);
        ResetIdleTimer();

        if (exitTextObject != null)
        {
            exitTextObject.SetActive(true);

            CanvasGroup cg = exitTextObject.GetComponent<CanvasGroup>();
            if (cg == null)
                cg = exitTextObject.AddComponent<CanvasGroup>();

            cg.alpha = 0f;
        }

        StartCoroutine(ExitGameRoutine());
    }

    private IEnumerator ExitGameRoutine()
    {
        isTransitioning = true;

        // 1. Затемнение экрана
        if (backgroundFade != null)
        {
            float fadeStart = backgroundFade.color.a;
            float fadeEnd = 1f;
            float t = 0f;

            while (t < exitFadeDuration)
            {
                t += Time.deltaTime;
                Color c = backgroundFade.color;
                c.a = Mathf.Lerp(fadeStart, fadeEnd, t / exitFadeDuration);
                backgroundFade.color = c;
                yield return null;
            }

            Color final = backgroundFade.color;
            final.a = fadeEnd;
            backgroundFade.color = final;
        }

        // 2. Проявление текста
        if (exitTextObject != null)
        {
            CanvasGroup cg = exitTextObject.GetComponent<CanvasGroup>();
            float textFadeDuration = 0.5f;
            float t = 0f;

            while (t < textFadeDuration)
            {
                t += Time.deltaTime;
                cg.alpha = Mathf.Lerp(0f, 1f, t / textFadeDuration);
                yield return null;
            }

            cg.alpha = 1f;
        }

        // 3. Пауза перед выходом
        yield return new WaitForSeconds(0.5f);

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ================= AUDIO =================
    private void PlayClickSound(AudioClip clip)
    {
        if (uiAudioSource == null || clip == null) return;
        uiAudioSource.PlayOneShot(clip);
    }

    private void PlayTransitionSound()
    {
        if (uiAudioSource != null && transitionSound != null)
            uiAudioSource.PlayOneShot(transitionSound);
    }

    private void PlayBackgroundMusic(int index)
    {
        if (musicAudioSource == null || backgroundMusic == null || backgroundMusic.Length == 0) return;
        if (index < 0 || index >= backgroundMusic.Length) return;

        musicAudioSource.clip = backgroundMusic[index];
        musicAudioSource.loop = true;
        musicAudioSource.volume = 1f;
        musicAudioSource.Play();
    }

    private IEnumerator FadeOutMusic()
    {
        if (musicAudioSource == null) yield break;

        float startVol = musicAudioSource.volume;
        float t = 0f;

        while (t < musicFadeDuration)
        {
            t += Time.deltaTime;
            musicAudioSource.volume = Mathf.Lerp(startVol, 0f, t / musicFadeDuration);
            yield return null;
        }

        musicAudioSource.volume = 0f;
        musicAudioSource.Stop();
    }

    // ================= FADE =================
    private IEnumerator FadeImage(float from, float to)
    {
        if (backgroundFade == null) yield break;

        float t = 0f;
        Color c = backgroundFade.color;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(from, to, t / fadeDuration);
            backgroundFade.color = c;
            yield return null;
        }

        c.a = to;
        backgroundFade.color = c;
    }

    private void SetFadeAlpha(float a)
    {
        if (backgroundFade == null) return;

        Color c = backgroundFade.color;
        c.a = a;
        backgroundFade.color = c;
    }

    // ================= SCENE LOAD =================
    public void LoadScene(AudioClip clickSound)
    {
        if (isTransitioning || string.IsNullOrEmpty(sceneToLoad)) return;
        StartCoroutine(LoadSceneRoutine(clickSound));
    }

    private IEnumerator LoadSceneRoutine(AudioClip clickSound)
    {
        isTransitioning = true;

        PlayClickSound(clickSound);
        PlayTransitionSound();

        yield return StartCoroutine(FadeImage(0f, 1f));
        yield return StartCoroutine(FadeOutMusic());

        SceneManager.LoadScene(sceneToLoad);
    }

    // ================= QUIT =================
    public void QuitGame(AudioClip clickSound)
    {
        PlayClickSound(clickSound);

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}