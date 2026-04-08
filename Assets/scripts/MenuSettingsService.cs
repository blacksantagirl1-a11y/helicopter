using UnityEngine;

public struct MenuSettingsData
{
    public float masterVolume;
    public float lookSensitivity;
    public bool fullscreen;
    public int qualityPreset;
}

public static class MenuSettingsService
{
    const string MasterVolumeKey = "menu.masterVolume";
    const string LookSensitivityKey = "menu.lookSensitivity";
    const string FullscreenKey = "menu.fullscreen";
    const string QualityPresetKey = "menu.qualityPreset";

    const float DefaultMasterVolume = 1f;
    const float DefaultLookSensitivity = 2f;
    const float MinLookSensitivity = 0.5f;
    const float MaxLookSensitivity = 8f;

    static bool hasLoadedCache;
    static MenuSettingsData cachedSettings;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        ApplyGlobalSettings(Load());
    }

    public static MenuSettingsData Load()
    {
        if (hasLoadedCache)
        {
            return cachedSettings;
        }

        int maxQualityIndex = Mathf.Max(0, QualitySettings.names.Length - 1);
        int defaultQualityIndex = Mathf.Clamp(QualitySettings.GetQualityLevel(), 0, maxQualityIndex);
        bool defaultFullscreen = Screen.fullScreenMode != FullScreenMode.Windowed;

        cachedSettings = new MenuSettingsData
        {
            masterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumeKey, DefaultMasterVolume)),
            lookSensitivity = Mathf.Clamp(PlayerPrefs.GetFloat(LookSensitivityKey, DefaultLookSensitivity), MinLookSensitivity, MaxLookSensitivity),
            fullscreen = PlayerPrefs.GetInt(FullscreenKey, defaultFullscreen ? 1 : 0) == 1,
            qualityPreset = Mathf.Clamp(PlayerPrefs.GetInt(QualityPresetKey, defaultQualityIndex), 0, maxQualityIndex)
        };

        hasLoadedCache = true;
        return cachedSettings;
    }

    public static void Save(MenuSettingsData settings)
    {
        cachedSettings = Clamp(settings);
        hasLoadedCache = true;

        PlayerPrefs.SetFloat(MasterVolumeKey, cachedSettings.masterVolume);
        PlayerPrefs.SetFloat(LookSensitivityKey, cachedSettings.lookSensitivity);
        PlayerPrefs.SetInt(FullscreenKey, cachedSettings.fullscreen ? 1 : 0);
        PlayerPrefs.SetInt(QualityPresetKey, cachedSettings.qualityPreset);
        PlayerPrefs.Save();

        ApplyGlobalSettings(cachedSettings);
    }

    public static float GetLookSensitivity()
    {
        return Load().lookSensitivity;
    }

    public static string GetDisplayModeLabel(bool fullscreen)
    {
        return fullscreen ? "Fullscreen" : "Windowed";
    }

    public static string GetQualityLabel(int qualityIndex)
    {
        string[] qualityNames = QualitySettings.names;
        if (qualityNames == null || qualityNames.Length == 0)
        {
            return "Default";
        }

        int clampedIndex = Mathf.Clamp(qualityIndex, 0, qualityNames.Length - 1);
        return qualityNames[clampedIndex];
    }

    static MenuSettingsData Clamp(MenuSettingsData settings)
    {
        int maxQualityIndex = Mathf.Max(0, QualitySettings.names.Length - 1);

        settings.masterVolume = Mathf.Clamp01(settings.masterVolume);
        settings.lookSensitivity = Mathf.Clamp(settings.lookSensitivity, MinLookSensitivity, MaxLookSensitivity);
        settings.qualityPreset = Mathf.Clamp(settings.qualityPreset, 0, maxQualityIndex);

        return settings;
    }

    static void ApplyGlobalSettings(MenuSettingsData settings)
    {
        settings = Clamp(settings);

        AudioListener.volume = settings.masterVolume;

        FullScreenMode targetMode = settings.fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        if (Screen.fullScreenMode != targetMode)
        {
            Screen.fullScreenMode = targetMode;
        }

        if (QualitySettings.GetQualityLevel() != settings.qualityPreset)
        {
            QualitySettings.SetQualityLevel(settings.qualityPreset, true);
        }
    }
}
