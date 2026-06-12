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
    private const string MasterVolumeKey = "menu.masterVolume";
    private const string LookSensitivityKey = "menu.lookSensitivity";
    private const string FullscreenKey = "menu.fullscreen";
    private const string QualityPresetKey = "menu.qualityPreset";
    private const string CheatModeKey = "menu.cheatMode";
    private const string SliderMasterVolumeKey = "MasterVolume";
    private const string MusicVolumeKey = "MusicVolume";
    private const string SfxVolumeKey = "SFXVolume";
    private const string AmbientVolumeKey = "AmbientVolume";

    private const float DefaultMasterVolume = 1f;
    private const float DefaultLookSensitivity = 2f;
    private const float MinLookSensitivity = 0.5f;
    private const float MaxLookSensitivity = 8f;

    private static bool hasLoadedCache;
    private static MenuSettingsData cachedSettings;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
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

    public static void SaveLookSensitivity(float sensitivity)
    {
        MenuSettingsData settings = Load();
        settings.lookSensitivity = sensitivity;
        Save(settings);
    }

    public static bool GetFullscreen()
    {
        return Load().fullscreen;
    }

    public static void SaveFullscreen(bool fullscreen)
    {
        MenuSettingsData settings = Load();
        settings.fullscreen = fullscreen;
        Save(settings);
    }

    public static bool GetCheatMode()
    {
        return PlayerPrefs.GetInt(CheatModeKey, 0) == 1;
    }

    public static void SaveCheatMode(bool enabled)
    {
        PlayerPrefs.SetInt(CheatModeKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    public static float GetMasterVolume(float fallback)
    {
        return PlayerPrefs.GetFloat(SliderMasterVolumeKey, fallback);
    }

    public static void SaveMasterVolume(float volume)
    {
        SaveSliderVolume(SliderMasterVolumeKey, volume);
    }

    public static float GetMusicVolume(float fallback)
    {
        return PlayerPrefs.GetFloat(MusicVolumeKey, fallback);
    }

    public static void SaveMusicVolume(float volume)
    {
        SaveSliderVolume(MusicVolumeKey, volume);
    }

    public static float GetSfxVolume(float fallback)
    {
        return PlayerPrefs.GetFloat(SfxVolumeKey, fallback);
    }

    public static void SaveSfxVolume(float volume)
    {
        SaveSliderVolume(SfxVolumeKey, volume);
    }

    public static float GetAmbientVolume(float fallback)
    {
        return PlayerPrefs.GetFloat(AmbientVolumeKey, fallback);
    }

    public static void SaveAmbientVolume(float volume)
    {
        SaveSliderVolume(AmbientVolumeKey, volume);
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

    private static MenuSettingsData Clamp(MenuSettingsData settings)
    {
        int maxQualityIndex = Mathf.Max(0, QualitySettings.names.Length - 1);

        settings.masterVolume = Mathf.Clamp01(settings.masterVolume);
        settings.lookSensitivity = Mathf.Clamp(settings.lookSensitivity, MinLookSensitivity, MaxLookSensitivity);
        settings.qualityPreset = Mathf.Clamp(settings.qualityPreset, 0, maxQualityIndex);

        return settings;
    }

    private static void SaveSliderVolume(string key, float volume)
    {
        PlayerPrefs.SetFloat(key, Mathf.Clamp(volume, 0f, 100f));
        PlayerPrefs.Save();
        AudioListener.volume = 1f;
    }

    private static void ApplyGlobalSettings(MenuSettingsData settings)
    {
        settings = Clamp(settings);

        AudioListener.volume = 1f;

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
