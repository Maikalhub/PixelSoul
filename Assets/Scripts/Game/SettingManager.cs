using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    [Header("Ссылка на ScriptableObject с настройками")]
    [SerializeField] private SettingsData settingsData;

    // Публичное свойство для быстрого доступа к данным
    public SettingsData Settings => settingsData;

    // Кэш уникальных разрешений монитора
    public List<Resolution> Resolutions { get; private set; }

    private string savePath;

    private void Awake()
    {
        // Синглтон – не уничтожается при загрузке новых сцен
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Путь к файлу сохранения
        savePath = Path.Combine(Application.persistentDataPath, "settings.json");

        // Заполняем список уникальных разрешений
        Resolutions = Screen.resolutions
            .Select(r => new Resolution { width = r.width, height = r.height })
            .Distinct()
            .ToList();

        // Загружаем сохранённые настройки (или создаём файл с дефолтными)
        LoadSettings();

        // Автоматически задаём текущее разрешение, если оно не было сохранено
        if (settingsData.resolutionIndex < 0 || settingsData.resolutionIndex >= Resolutions.Count)
        {
            Resolution current = Screen.currentResolution;
            int idx = Resolutions.FindIndex(r => r.width == current.width && r.height == current.height);
            if (idx >= 0)
                settingsData.resolutionIndex = idx;
            else if (Resolutions.Count > 0)
                settingsData.resolutionIndex = 0;
            SaveSettings();
        }

        // Сразу применяем все параметры
        ApplyAll();
    }

    // ================= ЗАГРУЗКА / СОХРАНЕНИЕ =================
    public void LoadSettings()
    {
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            JsonUtility.FromJsonOverwrite(json, settingsData);
        }
        else
        {
            // Файла нет – сохраняем значения по умолчанию из ScriptableObject
            SaveSettings();
        }
    }

    public void SaveSettings()
    {
        string json = JsonUtility.ToJson(settingsData, true);
        File.WriteAllText(savePath, json);
    }

    // ================= ПРИМЕНЕНИЕ ВСЕХ НАСТРОЕК =================
    public void ApplyAll()
    {
        ApplyFullscreenMode();
        ApplyQuality();
        ApplyResolution();
        ApplyVSync();
        ApplyTextureQuality();
        ApplyVolume();
    }

    // ================= МЕТОДЫ УСТАНОВКИ (вызываются из UI) =================
    public void SetLanguage(string lang)
    {
        if (settingsData.language != lang)
        {
            settingsData.language = lang;
            SaveSettings();
            // Здесь можно бросить событие для перевода интерфейса
        }
    }

    public void SetFullscreenMode(int mode)
    {
        settingsData.fullscreenMode = mode;
        ApplyFullscreenMode();
        SaveSettings();
    }

    public void SetShowFPS(bool show)
    {
        settingsData.showFPS = show;
        SaveSettings();
        // FPS-счётчик должен сам проверять settingsData.showFPS
    }

    public void SetQuality(int index)
    {
        settingsData.qualityLevel = index;
        ApplyQuality();
        SaveSettings();
    }

    public void SetResolutionIndex(int idx)
    {
        settingsData.resolutionIndex = idx;
        ApplyResolution();
        SaveSettings();
    }

    public void SetVSync(bool on)
    {
        settingsData.vsync = on;
        ApplyVSync();
        SaveSettings();
    }

    public void SetTextureQuality(int level)
    {
        settingsData.textureQuality = level;
        ApplyTextureQuality();
        SaveSettings();
    }

    public void SetMasterVolume(float v)
    {
        settingsData.masterVolume = Mathf.Clamp01(v);
        ApplyVolume();
        SaveSettings();
    }

    public void SetMusicVolume(float v)
    {
        settingsData.musicVolume = Mathf.Clamp01(v);
        ApplyVolume();
        SaveSettings();
    }

    public void SetSFXVolume(float v)
    {
        settingsData.sfxVolume = Mathf.Clamp01(v);
        ApplyVolume();
        SaveSettings();
    }

    public void SetControllerVibration(bool on)
    {
        settingsData.controllerVibration = on;
        SaveSettings();
    }

    public void SetSubtitles(bool on)
    {
        settingsData.subtitles = on;
        SaveSettings();
    }

    public void SetUIScale(float scale)
    {
        settingsData.uiScale = Mathf.Clamp(scale, 0.5f, 2f);
        SaveSettings();
        // Здесь можно применить масштаб к CanvasScaler
    }

    // ================= ПРИВАТНЫЕ МЕТОДЫ ПРИМЕНЕНИЯ =================
    private void ApplyFullscreenMode()
    {
        FullScreenMode mode;
        switch (settingsData.fullscreenMode)
        {
            case 0:
                mode = FullScreenMode.ExclusiveFullScreen;
                break;
            case 1:
                mode = FullScreenMode.FullScreenWindow;
                break;
            default:
                mode = FullScreenMode.Windowed;
                break;
        }

        // Устанавливаем режим
        Screen.fullScreenMode = mode;

        // Применяем разрешение
        if (settingsData.resolutionIndex >= 0 && settingsData.resolutionIndex < Resolutions.Count)
        {
            var res = Resolutions[settingsData.resolutionIndex];
            Screen.SetResolution(res.width, res.height, mode);
        }
        else
        {
            // Если индекс невалидный, используем текущее разрешение
            Screen.SetResolution(
                Screen.currentResolution.width,
                Screen.currentResolution.height,
                mode
            );
        }

        Debug.Log($"Применён режим экрана: {mode}, разрешение: {Screen.width}x{Screen.height}");
    }

    private void ApplyResolution()
    {
        if (settingsData.resolutionIndex < 0 || settingsData.resolutionIndex >= Resolutions.Count)
        {
            Debug.LogWarning($"Некорректный индекс разрешения: {settingsData.resolutionIndex}");
            return;
        }

        var res = Resolutions[settingsData.resolutionIndex];
        Screen.SetResolution(res.width, res.height, Screen.fullScreenMode);
        Debug.Log($"Применено разрешение: {res.width}x{res.height}");
    }

    private void ApplyQuality()
    {
        QualitySettings.SetQualityLevel(settingsData.qualityLevel, true);
        Debug.Log($"Применён уровень качества: {QualitySettings.names[settingsData.qualityLevel]}");
    }

    private void ApplyVSync()
    {
        QualitySettings.vSyncCount = settingsData.vsync ? 1 : 0;
        Debug.Log($"VSync: {(settingsData.vsync ? "Включен" : "Выключен")}");
    }

    private void ApplyTextureQuality()
    {
        // Инвертируем: 2 (High) -> mipmapLimit 0, 0 (Low) -> mipmapLimit 2
        QualitySettings.globalTextureMipmapLimit = 2 - settingsData.textureQuality;
        Debug.Log($"Качество текстур: {settingsData.textureQuality} (mipmap limit: {QualitySettings.globalTextureMipmapLimit})");
    }

    private void ApplyVolume()
    {
        // Общая громкость через AudioListener
        AudioListener.volume = settingsData.masterVolume;

        // Если используется AudioMixer – раскомментируй и настрой имена параметров:
        /*
        var mixer = Resources.Load<UnityEngine.Audio.AudioMixer>("MasterMixer");
        if (mixer != null)
        {
            mixer.SetFloat("MasterVolume", Mathf.Log10(settingsData.masterVolume) * 20);
            mixer.SetFloat("MusicVolume", Mathf.Log10(settingsData.musicVolume) * 20);
            mixer.SetFloat("SFXVolume", Mathf.Log10(settingsData.sfxVolume) * 20);
        }
        */

        Debug.Log($"Громкость: Master={settingsData.masterVolume:F2}, Music={settingsData.musicVolume:F2}, SFX={settingsData.sfxVolume:F2}");
    }

    // ================= ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ =================

    // Получить текущее разрешение как строку (для отладки)
    public string GetCurrentResolutionString()
    {
        return $"{Screen.width}x{Screen.height} @ {Screen.fullScreenMode}";
    }

    // Получить список доступных разрешений как строки
    public List<string> GetResolutionOptions()
    {
        return Resolutions.Select(r => $"{r.width}x{r.height}").ToList();
    }

    // Сбросить все настройки к значениям по умолчанию
    public void ResetToDefaults()
    {
        settingsData.language = "en";
        settingsData.fullscreenMode = 1;
        settingsData.showFPS = false;
        settingsData.qualityLevel = 2;
        settingsData.vsync = true;
        settingsData.textureQuality = 2;
        settingsData.masterVolume = 1f;
        settingsData.musicVolume = 0.8f;
        settingsData.sfxVolume = 1f;
        settingsData.controllerVibration = true;
        settingsData.subtitles = true;
        settingsData.uiScale = 1f;

        // Сбросим разрешение на текущее
        Resolution current = Screen.currentResolution;
        int idx = Resolutions.FindIndex(r => r.width == current.width && r.height == current.height);
        settingsData.resolutionIndex = idx >= 0 ? idx : 0;

        SaveSettings();
        ApplyAll();
        Debug.Log("Настройки сброшены к значениям по умолчанию");
    }
}