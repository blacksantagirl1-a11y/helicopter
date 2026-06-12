using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class SubMenuSettingsController : MonoBehaviour
{
    public AudioMixer audioMixer;
    public Slider masterSlider;
    public Slider musicSlider;
    public Slider sfxSlider;
    public Slider ambientSlider;
    public Slider mouseSensitivitySlider;
    public TMP_Text masterValueText;
    public TMP_Text musicValueText;
    public TMP_Text sfxValueText;
    public TMP_Text ambientValueText;
    public TMP_Text mouseSensitivityValueText;
    public Toggle cheatToggle;
    public AudioSource ambientSource;

    private bool isLoadingSettings;
    private bool listenersRegistered;

    private void Awake()
    {
        WireReferences();
        RegisterRuntimeListeners();
    }

    private void Start()
    {
        LoadSettings();
    }

    private void OnEnable()
    {
        WireReferences();
        RegisterRuntimeListeners();
        LoadSettings();
    }

    public void FullScreen()
    {
        SetFullscreenMode(true);
    }

    public void Window()
    {
        SetFullscreenMode(false);
    }

    public void UpdateMasterVolume(float volume)
    {
        SetSliderValueText(masterValueText, volume, true);
        if (!isLoadingSettings)
        {
            MenuSettingsService.SaveMasterVolume(volume);
        }

        if (musicSlider != null)
        {
            UpdateMusicVolume(musicSlider.value);
        }

        if (sfxSlider != null)
        {
            UpdateSoundVolume(sfxSlider.value);
        }

        if (ambientSlider != null)
        {
            UpdateAmbientVolume(ambientSlider.value);
        }
    }

    public void UpdateMusicVolume(float volume)
    {
        SetSliderValueText(musicValueText, volume, true);

        if (audioMixer != null)
        {
            audioMixer.SetFloat("MusicVolume", SliderValueToDecibels(GetEffectiveVolume(volume)));
        }

        if (!isLoadingSettings)
        {
            MenuSettingsService.SaveMusicVolume(volume);
        }
    }

    public void UpdateSoundVolume(float volume)
    {
        SetSliderValueText(sfxValueText, volume, true);

        if (audioMixer != null)
        {
            audioMixer.SetFloat("SFXVolume", SliderValueToDecibels(GetEffectiveVolume(volume)));
        }

        if (!isLoadingSettings)
        {
            MenuSettingsService.SaveSfxVolume(volume);
        }
    }

    public void UpdateAmbientVolume(float volume)
    {
        SetSliderValueText(ambientValueText, volume, true);
        ApplyAmbientVolume(volume);

        if (!isLoadingSettings)
        {
            MenuSettingsService.SaveAmbientVolume(volume);
        }
    }

    public void UpdateMouseSensitivity(float sensitivity)
    {
        SetSliderValueText(mouseSensitivityValueText, sensitivity, false);

        if (isLoadingSettings)
        {
            return;
        }

        MenuSettingsService.SaveLookSensitivity(sensitivity);
    }

    public void UpdateCheatMode(bool enabled)
    {
        if (!isLoadingSettings)
        {
            MenuSettingsService.SaveCheatMode(enabled);
        }
    }

    private void LoadSettings()
    {
        WireReferences();
        isLoadingSettings = true;

        if (masterSlider != null)
        {
            float masterVolume = MenuSettingsService.GetMasterVolume(masterSlider.value);
            masterSlider.SetValueWithoutNotify(masterVolume);
        }

        if (musicSlider != null)
        {
            float musicVolume = MenuSettingsService.GetMusicVolume(musicSlider.value);
            musicSlider.SetValueWithoutNotify(musicVolume);
        }

        if (sfxSlider != null)
        {
            float soundVolume = MenuSettingsService.GetSfxVolume(sfxSlider.value);
            sfxSlider.SetValueWithoutNotify(soundVolume);
        }

        if (ambientSlider != null)
        {
            float ambientVolume = MenuSettingsService.GetAmbientVolume(ambientSlider.value);
            ambientSlider.SetValueWithoutNotify(ambientVolume);
        }

        if (mouseSensitivitySlider != null)
        {
            float sensitivity = MenuSettingsService.GetLookSensitivity();
            mouseSensitivitySlider.SetValueWithoutNotify(sensitivity);
        }

        if (cheatToggle != null)
        {
            cheatToggle.SetIsOnWithoutNotify(MenuSettingsService.GetCheatMode());
        }

        if (masterSlider != null)
        {
            UpdateMasterVolume(masterSlider.value);
        }

        if (mouseSensitivitySlider != null)
        {
            UpdateMouseSensitivity(mouseSensitivitySlider.value);
        }

        isLoadingSettings = false;
    }

    private void WireReferences()
    {
        Slider[] sliders = GetComponentsInChildren<Slider>(true);
        for (int i = 0; i < sliders.Length; i++)
        {
            Slider slider = sliders[i];
            if (slider == null)
            {
                continue;
            }

            switch (slider.name)
            {
                case "MasterSlider":
                    masterSlider = masterSlider != null ? masterSlider : slider;
                    masterValueText = masterValueText != null ? masterValueText : FindValueText(slider.transform, "MasterValue");
                    break;
                case "MusicSlider":
                    musicSlider = musicSlider != null ? musicSlider : slider;
                    musicValueText = musicValueText != null ? musicValueText : FindValueText(slider.transform, "MusicValue");
                    break;
                case "SoundSlider":
                    sfxSlider = sfxSlider != null ? sfxSlider : slider;
                    sfxValueText = sfxValueText != null ? sfxValueText : FindValueText(slider.transform, "SoundValue");
                    break;
                case "AmbientSlider":
                    ambientSlider = ambientSlider != null ? ambientSlider : slider;
                    ambientValueText = ambientValueText != null ? ambientValueText : FindValueText(slider.transform, "AmbientValue");
                    break;
                case "MouseSlider":
                    mouseSensitivitySlider = mouseSensitivitySlider != null ? mouseSensitivitySlider : slider;
                    mouseSensitivityValueText = mouseSensitivityValueText != null ? mouseSensitivityValueText : FindValueText(slider.transform, "MouseSensitivityValue");
                    break;
            }
        }

        Toggle[] toggles = GetComponentsInChildren<Toggle>(true);
        for (int i = 0; i < toggles.Length; i++)
        {
            Toggle toggle = toggles[i];
            if (toggle == null)
            {
                continue;
            }

            if (toggle.name.ToLowerInvariant().Contains("cheat"))
            {
                cheatToggle = cheatToggle != null ? cheatToggle : toggle;
            }
        }
    }

    private void RegisterRuntimeListeners()
    {
        if (listenersRegistered)
        {
            return;
        }

        if (masterSlider != null)
        {
            masterSlider.onValueChanged.AddListener(UpdateMasterVolume);
        }

        if (musicSlider != null)
        {
            musicSlider.onValueChanged.AddListener(UpdateMusicVolume);
        }

        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.AddListener(UpdateSoundVolume);
        }

        if (ambientSlider != null)
        {
            ambientSlider.onValueChanged.AddListener(UpdateAmbientVolume);
        }

        if (mouseSensitivitySlider != null)
        {
            mouseSensitivitySlider.onValueChanged.AddListener(UpdateMouseSensitivity);
        }

        if (cheatToggle != null)
        {
            cheatToggle.onValueChanged.AddListener(UpdateCheatMode);
        }

        Button[] buttons = GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null)
            {
                continue;
            }

            if (button.name == "FullScreen")
            {
                button.onClick.AddListener(FullScreen);
            }
            else if (button.name == "Window")
            {
                button.onClick.AddListener(Window);
            }
        }

        listenersRegistered = true;
    }

    private TMP_Text FindValueText(Transform root, string valueName)
    {
        TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] != null && texts[i].name == valueName)
            {
                return texts[i];
            }
        }

        return null;
    }

    private void SetFullscreenMode(bool fullscreen)
    {
        FullScreenMode targetMode = fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        if (Screen.fullScreenMode != targetMode)
        {
            Screen.fullScreenMode = targetMode;
        }

        if (MenuSettingsService.GetFullscreen() != fullscreen)
        {
            MenuSettingsService.SaveFullscreen(fullscreen);
        }
    }

    private float GetEffectiveVolume(float channelVolume)
    {
        float masterVolume = masterSlider != null
            ? masterSlider.value
            : MenuSettingsService.GetMasterVolume(1f);

        return SliderValueToNormalized(channelVolume) * SliderValueToNormalized(masterVolume);
    }

    private float SliderValueToNormalized(float value)
    {
        if (value > 1f)
        {
            value /= 100f;
        }

        return Mathf.Clamp01(value);
    }

    private float SliderValueToDecibels(float value)
    {
        if (value <= 0.0001f)
        {
            return -80f;
        }

        return Mathf.Log10(value) * 20f;
    }

    private void ApplyAmbientVolume(float volume)
    {
        float effectiveVolume = GetEffectiveVolume(volume);
        AudioSource[] sources = FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < sources.Length; i++)
        {
            AudioSource source = sources[i];
            if (source == null || source.name != "AmbientSource")
            {
                continue;
            }

            ambientSource = source;
            source.volume = effectiveVolume;

            if (source.clip != null && source.loop && !source.isPlaying)
            {
                source.Play();
            }
        }
    }

    private AudioSource ResolveAmbientSource()
    {
        if (ambientSource != null)
        {
            return ambientSource;
        }

        GameObject ambientObject = GameObject.Find("AmbientSource");
        if (ambientObject != null)
        {
            ambientSource = ambientObject.GetComponent<AudioSource>();
        }

        return ambientSource;
    }

    private void SetSliderValueText(TMP_Text valueText, float value, bool wholeNumber)
    {
        if (valueText == null)
        {
            return;
        }

        valueText.text = wholeNumber
            ? Mathf.RoundToInt(value).ToString()
            : value.ToString("0.0");
    }
}
