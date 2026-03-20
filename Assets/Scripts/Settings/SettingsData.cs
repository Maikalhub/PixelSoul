using UnityEngine;

[CreateAssetMenu(fileName = "SettingsData", menuName = "Settings/SettingsData")]
public class SettingsData : ScriptableObject
{
    [Range(0f, 1f)] public float soundVolume = 1f;       // громкость
    [Range(0f, 1f)] public float graphicsQuality = 1f;   // качество графики
}
