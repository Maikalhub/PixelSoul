using UnityEngine;

[CreateAssetMenu(fileName = "SettingsData", menuName = "Settings/SettingsData")]
public class SettingsData : ScriptableObject
{
    [Header("Главные")]
    public string language = "en";                     // "en", "ru", "de"
    public int fullscreenMode = 1;                    // 0=Exclusive, 1=Borderless, 2=Windowed
    public bool showFPS = false;

    [Header("Графика")]
    public int qualityLevel = 2;                      // индекс из QualitySettings.names
    public int resolutionIndex = -1;                  // индекс в списке уникальных разрешений
    // Убрано: public bool postProcessing = true;
    public bool vsync = true;
    [Range(0, 2)] public int textureQuality = 2;     // 0=Low, 1=Med, 2=High

    [Header("Звук")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float musicVolume = 0.8f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    [Header("Другие")]
    public bool controllerVibration = true;
    public bool subtitles = true;
    [Range(0.5f, 2f)] public float uiScale = 1f;
}