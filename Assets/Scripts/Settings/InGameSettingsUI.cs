using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class InGameSettingsUI : MonoBehaviour
{
    [Header("SETTINGS TABS")]
    [SerializeField] private GameObject[] settingsTabs;
    [SerializeField] private int defaultSettingsTab = 0;

    // ========== UI ЭЛЕМЕНТЫ ==========
    [Header("Главные (General)")]
    [SerializeField] private TMP_Dropdown languageDropdown;
    [SerializeField] private TMP_Dropdown fullscreenModeDropdown;
    [SerializeField] private Toggle showFPSToggle;

    [Header("Графика")]
    [SerializeField] private TMP_Dropdown qualityDropdown;
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private Toggle vsyncToggle;
    [SerializeField] private Slider textureQualitySlider;

    [Header("Звук")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;

    [Header("Другие")]
    [SerializeField] private Toggle vibrationToggle;
    [SerializeField] private Toggle subtitlesToggle;
    [SerializeField] private Slider uiScaleSlider;

    private Dictionary<string, string> languages = new Dictionary<string, string>
    {
        { "en", "English" },
        { "ru", "Русский" },
        { "de", "Deutsch" }
    };

    private bool isInitialized = false;

    // ================= UNITY =================
    private void Start()
    {
        InitializeSettings();
    }

    private void OnEnable()
    {
        // При активации объекта обновляем UI и показываем вкладку по умолчанию
        if (isInitialized)
        {
            ShowSettingsTab(defaultSettingsTab);
            RefreshUI();
        }
    }

    // ================= ВКЛАДКИ =================
    public void ShowSettingsTab(int index)
    {
        if (settingsTabs == null || settingsTabs.Length == 0) return;
        if (index < 0 || index >= settingsTabs.Length) return;

        for (int i = 0; i < settingsTabs.Length; i++)
        {
            if (settingsTabs[i] != null)
                settingsTabs[i].SetActive(i == index);
        }
    }

    // Методы для кнопок вкладок (привязываются в инспекторе к onClick)
    public void TabGeneral() => ShowSettingsTab(0);
    public void TabGraphics() => ShowSettingsTab(1);
    public void TabSound() => ShowSettingsTab(2);
    public void TabOther() => ShowSettingsTab(3);

    // ================= ИНИЦИАЛИЗАЦИЯ =================
    private void InitializeSettings()
    {
        if (SettingsManager.Instance == null)
        {
            Debug.LogError("SettingsManager не найден в сцене!");
            return;
        }

        if (languageDropdown == null || qualityDropdown == null || resolutionDropdown == null)
        {
            Debug.LogError("Не все UI-элементы настроек назначены в InGameSettingsUI!");
            return;
        }

        // Заполняем дропдауны
        PopulateLanguageDropdown();
        PopulateFullscreenDropdown();
        PopulateQualityDropdown();
        PopulateResolutionDropdown();

        // Настройка слайдеров
        if (textureQualitySlider != null)
        {
            textureQualitySlider.minValue = 0;
            textureQualitySlider.maxValue = 2;
            textureQualitySlider.wholeNumbers = true;
        }

        if (uiScaleSlider != null)
        {
            uiScaleSlider.minValue = 0.5f;
            uiScaleSlider.maxValue = 2f;
        }

        // Подписываемся на изменения
        languageDropdown.onValueChanged.AddListener(OnLanguageChanged);
        fullscreenModeDropdown.onValueChanged.AddListener(OnFullscreenModeChanged);
        showFPSToggle.onValueChanged.AddListener(OnShowFPSChanged);

        qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
        resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
        vsyncToggle.onValueChanged.AddListener(OnVSyncChanged);
        textureQualitySlider.onValueChanged.AddListener(OnTextureQualityChanged);

        masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);

        vibrationToggle.onValueChanged.AddListener(OnVibrationChanged);
        subtitlesToggle.onValueChanged.AddListener(OnSubtitlesChanged);
        uiScaleSlider.onValueChanged.AddListener(OnUIScaleChanged);

        isInitialized = true;
        RefreshUI();
    }

    // ================= ЗАПОЛНЕНИЕ ДРОПДАУНОВ =================
    private void PopulateLanguageDropdown()
    {
        languageDropdown.ClearOptions();
        languageDropdown.AddOptions(languages.Values.ToList());
    }

    private void PopulateFullscreenDropdown()
    {
        fullscreenModeDropdown.ClearOptions();
        fullscreenModeDropdown.AddOptions(new List<string> { "Exclusive Fullscreen", "Borderless Window", "Windowed" });
    }

    private void PopulateQualityDropdown()
    {
        qualityDropdown.ClearOptions();
        qualityDropdown.AddOptions(QualitySettings.names.ToList());
    }

    private void PopulateResolutionDropdown()
    {
        resolutionDropdown.ClearOptions();
        var options = new List<string>();
        foreach (var res in SettingsManager.Instance.Resolutions)
            options.Add($"{res.width} x {res.height}");
        resolutionDropdown.AddOptions(options);
    }

    // ================= ОБНОВЛЕНИЕ UI =================
    private void RefreshUI()
    {
        if (!isInitialized || SettingsManager.Instance == null) return;

        var s = SettingsManager.Instance.Settings;

        // Главные
        SetLanguageDropdownValue(s.language);
        fullscreenModeDropdown.value = s.fullscreenMode;
        showFPSToggle.isOn = s.showFPS;

        // Графика
        qualityDropdown.value = s.qualityLevel;
        SetResolutionDropdownValue();
        vsyncToggle.isOn = s.vsync;
        textureQualitySlider.value = s.textureQuality;

        // Звук
        masterVolumeSlider.value = s.masterVolume;
        musicVolumeSlider.value = s.musicVolume;
        sfxVolumeSlider.value = s.sfxVolume;

        // Другие
        vibrationToggle.isOn = s.controllerVibration;
        subtitlesToggle.isOn = s.subtitles;
        uiScaleSlider.value = s.uiScale;
    }

    private void SetLanguageDropdownValue(string langKey)
    {
        int idx = languages.Keys.ToList().IndexOf(langKey);
        if (idx >= 0) languageDropdown.value = idx;
    }

    private void SetResolutionDropdownValue()
    {
        int idx = SettingsManager.Instance.Settings.resolutionIndex;
        if (idx >= 0 && idx < resolutionDropdown.options.Count)
            resolutionDropdown.value = idx;
        else
            resolutionDropdown.value = 0;
    }

    // ================= ОБРАБОТЧИКИ ИЗМЕНЕНИЙ =================
    private void OnLanguageChanged(int idx)
    {
        if (!isInitialized) return;
        string key = languages.Keys.ElementAt(idx);
        SettingsManager.Instance.SetLanguage(key);
    }

    private void OnFullscreenModeChanged(int mode)
    {
        if (isInitialized) SettingsManager.Instance.SetFullscreenMode(mode);
    }

    private void OnShowFPSChanged(bool on)
    {
        if (isInitialized) SettingsManager.Instance.SetShowFPS(on);
    }

    private void OnQualityChanged(int idx)
    {
        if (isInitialized) SettingsManager.Instance.SetQuality(idx);
    }

    private void OnResolutionChanged(int idx)
    {
        if (isInitialized) SettingsManager.Instance.SetResolutionIndex(idx);
    }

    private void OnVSyncChanged(bool on)
    {
        if (isInitialized) SettingsManager.Instance.SetVSync(on);
    }

    private void OnTextureQualityChanged(float v)
    {
        if (isInitialized) SettingsManager.Instance.SetTextureQuality((int)v);
    }

    private void OnMasterVolumeChanged(float v)
    {
        if (isInitialized) SettingsManager.Instance.SetMasterVolume(v);
    }

    private void OnMusicVolumeChanged(float v)
    {
        if (isInitialized) SettingsManager.Instance.SetMusicVolume(v);
    }

    private void OnSFXVolumeChanged(float v)
    {
        if (isInitialized) SettingsManager.Instance.SetSFXVolume(v);
    }

    private void OnVibrationChanged(bool on)
    {
        if (isInitialized) SettingsManager.Instance.SetControllerVibration(on);
    }

    private void OnSubtitlesChanged(bool on)
    {
        if (isInitialized) SettingsManager.Instance.SetSubtitles(on);
    }

    private void OnUIScaleChanged(float s)
    {
        if (isInitialized) SettingsManager.Instance.SetUIScale(s);
    }
}