using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class MainMenu : MonoBehaviour
{

    // Ten track nhac nen duoc phat rieng cho man hinh main menu.
    private const string MainMenuMusicTrackName = "MainMenu";

    // Nut load game can duoc truy cap de gan them su kien dung nhac menu.
    public Button LoadGameBTN;

    // Thong so tao hieu ung hover cho cac nut menu bang DOTween.
    [SerializeField, Min(1f)] private float buttonHighlightScale = 1.06f;
    [SerializeField, Min(0f)] private float buttonTweenDuration = 0.16f;
    [SerializeField] private Color buttonNormalColor = new Color(0.92f, 0.96f, 1f, 0.78f);
    [SerializeField] private Color buttonHoverColor = Color.white;

    // Luu cac nut va thanh phan hien thi cua nut de cap nhat mau/scale khi hover.
    private readonly Dictionary<Button, MainMenuButtonVisual> menuButtons = new Dictionary<Button, MainMenuButtonVisual>();
    private Button hoveredButton;

    // Chan viec ghi PlayerPrefs nguoc lai trong luc dang nap setting len UI.
    private bool isLoadingSettings;

    private void Awake()
    {
        // Main menu can hien chuot va khong khoa camera nhan vat.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        RegisterMenuButtons();
        RefreshMenuButtons(true);
    }

    private void Start()
    {
        // Nap cau hinh am thanh/hien thi da luu va bat nhac nen main menu.
        LoadVolume();
        MusicManager.Instance?.PlayMusic(MainMenuMusicTrackName);

        if (LoadGameBTN != null)
        {
            LoadGameBTN.onClick.AddListener(StopMainMenuMusic);
        }
    }

    private void OnDestroy()
    {
        StopMainMenuMusic();
    }

    public void NewGame()
    {
        // Game moi luon reset tien trinh hoi thoai ve ngay 1 truoc khi vao scene chinh.
        DialogueNewGameResetService.ResetToDay1();
        StopMainMenuMusic();
        if (!LoadingManager.LoadScene("InGame"))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("InGame");
        }
    }

    private void StopMainMenuMusic()
    {
        MusicManager.Instance?.StopMusic(0f);
    }

    public void ExitGame()
    {
        Debug.Log("Quitting Game");
        Application.Quit();
    }

    private void RegisterMenuButtons()
    {
        // Tu dong tim tat ca Button con de gan hover relay, tranh phai gan thu cong tung nut.
        menuButtons.Clear();

        Button[] buttons = GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null)
            {
                continue;
            }

            menuButtons[button] = new MainMenuButtonVisual
            {
                button = button,
                label = button.GetComponentInChildren<TMP_Text>(true),
                rect = button.transform as RectTransform
            };

            MainMenuButtonHoverRelay relay = button.GetComponent<MainMenuButtonHoverRelay>();
            if (relay == null)
            {
                relay = button.gameObject.AddComponent<MainMenuButtonHoverRelay>();
            }

            relay.Initialize(button, OnMenuButtonHoverChanged);
        }
    }

    private void OnMenuButtonHoverChanged(Button button, bool isHovered)
    {
        if (isHovered)
        {
            hoveredButton = button;
        }
        else if (hoveredButton == button)
        {
            hoveredButton = null;
        }

        RefreshMenuButtons();
    }

    private void RefreshMenuButtons(bool immediate = false)
    {
        // Cap nhat toan bo nut theo nut dang duoc hover de tranh trang thai UI bi lech.
        foreach (MainMenuButtonVisual visual in menuButtons.Values)
        {
            bool isHovered = visual.button == hoveredButton;
            float targetScale = isHovered ? buttonHighlightScale : 1f;
            Color targetColor = isHovered ? buttonHoverColor : buttonNormalColor;

            ApplyMenuButtonVisual(visual, targetScale, targetColor, immediate);
        }
    }

    private void ApplyMenuButtonVisual(MainMenuButtonVisual visual, float targetScale, Color targetColor, bool immediate)
    {
        // DOKill dam bao tween cu dung lai truoc khi tao tween moi.
        if (visual.rect != null)
        {
            visual.rect.DOKill();
            if (immediate)
            {
                visual.rect.localScale = Vector3.one * targetScale;
            }
            else
            {
                visual.rect
                    .DOScale(targetScale, buttonTweenDuration)
                    .SetEase(Ease.OutQuad)
                    .SetUpdate(true);
            }
        }

        if (visual.label != null)
        {
            visual.label.DOKill();
            if (immediate)
            {
                visual.label.color = targetColor;
            }
            else
            {
                visual.label
                    .DOColor(targetColor, buttonTweenDuration)
                    .SetEase(Ease.OutQuad)
                    .SetUpdate(true);
            }
        }
    }

    private sealed class MainMenuButtonVisual
    {
        public Button button;
        public TMP_Text label;
        public RectTransform rect;
    }

    public AudioMixer audioMixer;
    public Slider masterSlider;
    public Slider musicSlider;
    public Slider sfxSlider;
    public Slider ambientSlider;
    public Slider mouseSensitivitySlider;
    public TMP_Text ambientValueText;
    public TMP_Text mouseSensitivityValueText;
    public AudioSource ambientSource;
 
    public void Play()
    {
        NewGame();
    }
 
    public void Quit()
    {
        ExitGame();
    }

    public void FullScreen()
    {
        // Nut UI goi ham nay de chuyen sang che do toan man hinh.
        SetFullscreenMode(true);
    }

    public void Window()
    {
        // Nut UI goi ham nay de chuyen sang che do cua so.
        SetFullscreenMode(false);
    }
 
    public void UpdateMusicVolume(float volume)
    {
        // Music/SFX duoc nhan voi master volume roi doi sang decibel cho AudioMixer.
        if (audioMixer != null)
        {
            audioMixer.SetFloat("MusicVolume", SliderValueToDecibels(GetEffectiveVolume(volume), null));
        }

        if (!isLoadingSettings)
        {
            MenuSettingsService.SaveMusicVolume(volume);
        }
    }
 
    public void UpdateSoundVolume(float volume)
    {
        if (audioMixer != null)
        {
            audioMixer.SetFloat("SFXVolume", SliderValueToDecibels(GetEffectiveVolume(volume), null));
        }

        if (!isLoadingSettings)
        {
            MenuSettingsService.SaveSfxVolume(volume);
        }
    }

    public void UpdateAmbientVolume(float volume)
    {
        // Ambient la AudioSource trong scene nen cap nhat truc tiep volume cua source.
        ApplyAmbientVolume(volume);

        SetSliderValueText(ambientValueText, volume, true);
        if (!isLoadingSettings)
        {
            MenuSettingsService.SaveAmbientVolume(volume);
        }
    }

    public void UpdateMasterVolume(float volume)
    {
        // Khi master doi, tinh lai tat ca kenh con de hieu ung master co tac dung ngay.
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

    public void UpdateMouseSensitivity(float sensitivity)
    {
        SetSliderValueText(mouseSensitivityValueText, sensitivity, false);

        if (!isLoadingSettings)
        {
            MenuSettingsService.SaveLookSensitivity(sensitivity);
        }
    }
 
    public void SaveVolume()
    {
        if (masterSlider != null)
        {
            MenuSettingsService.SaveMasterVolume(masterSlider.value);
        }

        if (musicSlider != null)
        {
            MenuSettingsService.SaveMusicVolume(musicSlider.value);
        }

        if (sfxSlider != null)
        {
            MenuSettingsService.SaveSfxVolume(sfxSlider.value);
        }

        if (ambientSlider != null)
        {
            MenuSettingsService.SaveAmbientVolume(ambientSlider.value);
        }

        if (mouseSensitivitySlider != null)
        {
            UpdateMouseSensitivity(mouseSensitivitySlider.value);
        }

        PlayerPrefs.Save();
    }
 
    public void LoadVolume()
    {
        // Nap gia tri da luu len slider ma khong kich hoat onValueChanged lap lai.
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

    private float SliderValueToDecibels(float value, Slider slider)
    {
        // AudioMixer dung don vi dB, slider 0 duoc coi nhu mute.
        float normalizedValue = value;
        if (slider != null && !Mathf.Approximately(slider.minValue, slider.maxValue))
        {
            normalizedValue = Mathf.InverseLerp(slider.minValue, slider.maxValue, value);
        }

        if (normalizedValue <= 0.0001f)
        {
            return -80f;
        }

        return Mathf.Log10(normalizedValue) * 20f;
    }

    private float GetEffectiveVolume(float channelVolume)
    {
        // Volume thuc te = volume kenh rieng * master volume.
        float masterVolume = masterSlider != null
            ? masterSlider.value
            : MenuSettingsService.GetMasterVolume(1f);

        return SliderValueToNormalized(channelVolume, null) * SliderValueToNormalized(masterVolume, masterSlider);
    }

    private float SliderValueToNormalized(float value, Slider slider)
    {
        if (slider != null && !Mathf.Approximately(slider.minValue, slider.maxValue))
        {
            return Mathf.InverseLerp(slider.minValue, slider.maxValue, value);
        }

        if (value > 1f)
        {
            value /= 100f;
        }

        return Mathf.Clamp01(value);
    }

    private void ApplyAmbientVolume(float volume)
    {
        // Tim cac AudioSource ten AmbientSource trong scene de ap dung setting ambient.
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

    private void SetFullscreenMode(bool fullscreen)
    {
        // Luu lua chon fullscreen de lan sau mo game van dung cau hinh cu.
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
