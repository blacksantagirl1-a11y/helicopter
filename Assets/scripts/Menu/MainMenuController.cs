using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    // File nay dieu khien toan bo man hinh menu chinh.
    // Doc file theo 4 y de hieu:
    // 1. Tim reference den button, panel, slider.
    // 2. Gan su kien click / thay doi gia tri.
    // 3. Mo dong panel Options / Credits bang tween.
    // 4. Luu setting va load scene gameplay khi bam Play.
    enum MenuState
    {
        Idle,
        OptionsOpen,
        CreditsOpen,
        Loading
    }

    const string DefaultGameplaySceneName = "Suml";

    [Header("Scene Flow")]
    [Tooltip("Tên scene gameplay sẽ được load khi bấm Play")]
    [SerializeField] string gameplaySceneName = DefaultGameplaySceneName;

    [Header("Canvas")]
    [Tooltip("CanvasGroup tổng để fade toàn bộ UI menu")]
    [SerializeField] CanvasGroup uiCanvasGroup;

    [Header("Primary Buttons")]
    [Tooltip("Nút bắt đầu game")]
    [SerializeField] Button playButton;
    [Tooltip("Text hiển thị trên nút Play")]
    [SerializeField] TMP_Text playButtonLabel;
    [Tooltip("Nút mở bảng Credits")]
    [SerializeField] Button creditsButton;
    [Tooltip("Text hiển thị trên nút Credits")]
    [SerializeField] TMP_Text creditsButtonLabel;
    [Tooltip("Nút mở bảng Options")]
    [SerializeField] Button optionsButton;
    [Tooltip("Text hiển thị trên nút Options")]
    [SerializeField] TMP_Text optionsButtonLabel;
    [Tooltip("Nút thoát game")]
    [SerializeField] Button quitButton;
    [Tooltip("Text hiển thị trên nút Quit")]
    [SerializeField] TMP_Text quitButtonLabel;

    [Header("Panels")]
    [Tooltip("Panel options")]
    [SerializeField] RectTransform optionsPanel;
    [Tooltip("CanvasGroup của panel options để animate alpha/interactable")]
    [SerializeField] CanvasGroup optionsPanelCanvasGroup;
    [Tooltip("Nút quay lại trong panel options")]
    [SerializeField] Button optionsBackButton;
    [Tooltip("Panel credits")]
    [SerializeField] RectTransform creditsPanel;
    [Tooltip("CanvasGroup của panel credits để animate alpha/interactable")]
    [SerializeField] CanvasGroup creditsPanelCanvasGroup;
    [Tooltip("Nút quay lại trong panel credits")]
    [SerializeField] Button creditsBackButton;

    [Header("Options")]
    [Tooltip("Slider âm lượng tổng")]
    [SerializeField] Slider volumeSlider;
    [Tooltip("Text hiển thị giá trị âm lượng")]
    [SerializeField] TMP_Text volumeValueText;
    [Tooltip("Slider độ nhạy chuột")]
    [SerializeField] Slider sensitivitySlider;
    [Tooltip("Text hiển thị giá trị độ nhạy")]
    [SerializeField] TMP_Text sensitivityValueText;
    [Tooltip("Nút đổi chế độ hiển thị (fullscreen/windowed)")]
    [SerializeField] Button displayModeButton;
    [Tooltip("Text hiển thị chế độ hiển thị hiện tại")]
    [SerializeField] TMP_Text displayModeValueText;
    [Tooltip("Nút giảm chất lượng đồ họa")]
    [SerializeField] Button qualityDecreaseButton;
    [Tooltip("Nút tăng chất lượng đồ họa")]
    [SerializeField] Button qualityIncreaseButton;
    [Tooltip("Text hiển thị mức chất lượng đồ họa")]
    [SerializeField] TMP_Text qualityValueText;

    [Header("Animation")]
    [Tooltip("Thời gian fade vào UI menu lúc mở màn")]
    [SerializeField, Min(0f)] float introFadeDuration = 0.45f;
    [Tooltip("Thời gian tween mở/đóng panel")]
    [SerializeField, Min(0f)] float panelTweenDuration = 0.35f;
    [Tooltip("Khoảng trượt panel khi ẩn")]
    [SerializeField, Min(0f)] float panelSlidePadding = 140f;
    [Tooltip("Lề phải của panel khi hiển thị")]
    [SerializeField, Min(0f)] float panelRightInset = 72f;
    [Tooltip("Tỷ lệ phóng nhẹ khi nút được highlight")]
    [SerializeField, Min(1f)] float highlightScale = 1.06f;
    [Tooltip("Thời gian tween hiệu ứng nút")]
    [SerializeField, Min(0f)] float buttonTweenDuration = 0.16f;

    [Header("Palette")]
    [Tooltip("Màu nút ở trạng thái bình thường")]
    [SerializeField] Color buttonNormalColor = new(0.92f, 0.96f, 1f, 0.78f);
    [Tooltip("Màu nút khi hover")]
    [SerializeField] Color buttonHoverColor = Color.white;
    [Tooltip("Màu nút khi bị khóa chọn")]
    [SerializeField] Color buttonLockedColor = new(1f, 0.96f, 0.82f, 1f);
    [Tooltip("Màu nút khi disabled")]
    [SerializeField] Color buttonDisabledColor = new(1f, 1f, 1f, 0.35f);

    readonly Dictionary<Button, MainButtonVisual> mainButtons = new();

    MenuSettingsData currentSettings;
    MenuState currentState;
    Button hoveredButton;
    Button lockedButton;
    Tween introTween;
    Tween activePanelTween;
    Vector2 optionsShownPosition;
    Vector2 optionsHiddenPosition;
    Vector2 creditsShownPosition;
    Vector2 creditsHiddenPosition;
    bool uiEventsBound;

    // Awake la buoc "lap day day du": nap setting, tim reference va bind UI.
    void Awake()
    {
        currentSettings = MenuSettingsService.Load();

        ResolveReferences();
        RegisterMainButtons();
        AttachHoverRelay(playButton);
        AttachHoverRelay(creditsButton);
        AttachHoverRelay(optionsButton);
        AttachHoverRelay(quitButton);

        UnlockCursor();
        ConfigurePanel(optionsPanel, optionsPanelCanvasGroup, out optionsShownPosition, out optionsHiddenPosition);
        ConfigurePanel(creditsPanel, creditsPanelCanvasGroup, out creditsShownPosition, out creditsHiddenPosition);
        ResetUiState();
        RefreshSettingsUi();
        BindUiEvents();
    }

    void OnDestroy()
    {
        UnbindUiEvents();
        KillTweens();
        StopMenuMusic();
    }

    void Start()
    {
        StartIntroFade();
        StartCoroutine(SelectButtonNextFrame(playButton));
        StartCoroutine(PlayMusicDelayed());
    }

    IEnumerator PlayMusicDelayed()
    {
        yield return null;
        Play();
    }

    void BindUiEvents()
    {
        if (uiEventsBound)
        {
            return;
        }

        BindButton(playButton, OnPlayPressed);
        BindButton(creditsButton, OpenCreditsPanel);
        BindButton(optionsButton, OpenOptionsPanel);
        BindButton(quitButton, QuitGame);
        BindButton(optionsBackButton, CloseOptionsPanel);
        BindButton(creditsBackButton, CloseCreditsPanel);
        BindButton(displayModeButton, ToggleDisplayMode);
        BindButton(qualityDecreaseButton, OnQualityDecreasePressed);
        BindButton(qualityIncreaseButton, OnQualityIncreasePressed);
        BindSlider(volumeSlider, OnVolumeChanged);
        BindSlider(sensitivitySlider, OnSensitivityChanged);
        uiEventsBound = true;
    }

    void UnbindUiEvents()
    {
        if (!uiEventsBound)
        {
            return;
        }

        UnbindButton(playButton, OnPlayPressed);
        UnbindButton(creditsButton, OpenCreditsPanel);
        UnbindButton(optionsButton, OpenOptionsPanel);
        UnbindButton(quitButton, QuitGame);
        UnbindButton(optionsBackButton, CloseOptionsPanel);
        UnbindButton(creditsBackButton, CloseCreditsPanel);
        UnbindButton(displayModeButton, ToggleDisplayMode);
        UnbindButton(qualityDecreaseButton, OnQualityDecreasePressed);
        UnbindButton(qualityIncreaseButton, OnQualityIncreasePressed);
        UnbindSlider(volumeSlider, OnVolumeChanged);
        UnbindSlider(sensitivitySlider, OnSensitivityChanged);
        uiEventsBound = false;
    }

    void ResolveReferences()
    {
        // Neu Inspector chua keo du component, script se thu tim theo ten object.
        uiCanvasGroup ??= GetComponentInChildren<CanvasGroup>(true);

        optionsPanelCanvasGroup ??= optionsPanel != null ? optionsPanel.GetComponent<CanvasGroup>() : null;
        creditsPanelCanvasGroup ??= creditsPanel != null ? creditsPanel.GetComponent<CanvasGroup>() : null;

        playButton ??= FindButton("PLAYButton");
        creditsButton ??= FindButton("CREDITSButton");
        optionsButton ??= FindButton("OPTIONSButton");
        quitButton ??= FindButton("QUITButton");

        playButtonLabel = ResolveLabel(playButton, playButtonLabel);
        creditsButtonLabel = ResolveLabel(creditsButton, creditsButtonLabel);
        optionsButtonLabel = ResolveLabel(optionsButton, optionsButtonLabel);
        quitButtonLabel = ResolveLabel(quitButton, quitButtonLabel);

        optionsBackButton ??= FindInPanel<Button>(optionsPanel, "BackButton");
        creditsBackButton ??= FindInPanel<Button>(creditsPanel, "BackButton");

        volumeSlider ??= FindInPanel<Slider>(optionsPanel, "Slider");
        sensitivitySlider ??= FindInPanel<Slider>(optionsPanel, "Slider");
        volumeValueText ??= FindInPanel<TMP_Text>(optionsPanel, "Value");
        sensitivityValueText ??= FindNextValueText(optionsPanel, volumeValueText);

        displayModeButton ??= FindInPanel<Button>(optionsPanel, "WindowedButton");
        displayModeValueText ??= ResolveLabel(displayModeButton, displayModeValueText);
        qualityDecreaseButton ??= FindInPanel<Button>(optionsPanel, "-Button");
        qualityIncreaseButton ??= FindInPanel<Button>(optionsPanel, "+Button");
        qualityValueText ??= FindInPanel<TMP_Text>(optionsPanel, "QualityValue");
    }

    // mainButtons la danh sach cac nut lon tren menu chinh,
    // giup ta doi mau va scale dong bo khi hover / lock.
    void RegisterMainButtons()
    {
        mainButtons.Clear();
        RegisterMainButton(playButton, playButtonLabel);
        RegisterMainButton(creditsButton, creditsButtonLabel);
        RegisterMainButton(optionsButton, optionsButtonLabel);
        RegisterMainButton(quitButton, quitButtonLabel);
    }

    void RegisterMainButton(Button button, TMP_Text label)
    {
        if (button == null)
        {
            return;
        }

        mainButtons[button] = new MainButtonVisual
        {
            button = button,
            label = label,
            rect = button.transform as RectTransform
        };
    }

    void AttachHoverRelay(Button button)
    {
        if (button == null)
        {
            return;
        }

        MainMenuButtonHoverRelay relay = button.GetComponent<MainMenuButtonHoverRelay>();
        if (relay == null)
        {
            relay = button.gameObject.AddComponent<MainMenuButtonHoverRelay>();
        }

        relay.Initialize(button, OnMainButtonHoverChanged);
    }

    void ResetUiState()
    {
        currentState = MenuState.Idle;
        hoveredButton = null;
        lockedButton = null;

        if (uiCanvasGroup != null)
        {
            uiCanvasGroup.alpha = 0f;
            uiCanvasGroup.interactable = true;
            uiCanvasGroup.blocksRaycasts = true;
        }

        HidePanelImmediate(optionsPanel, optionsPanelCanvasGroup, optionsHiddenPosition);
        HidePanelImmediate(creditsPanel, creditsPanelCanvasGroup, creditsHiddenPosition);
        SetMainButtonsInteractable(true);
        RefreshMainButtons(true);
    }

    // Fade nhe menu vao de mo man hinh trong "mem" hon, khong xuat hien dot ngot.
    void StartIntroFade()
    {
        if (uiCanvasGroup == null)
        {
            return;
        }

        introTween?.Kill();
        uiCanvasGroup.alpha = 0f;
        introTween = uiCanvasGroup
            .DOFade(1f, Mathf.Max(0.01f, introFadeDuration))
            .SetEase(Ease.OutCubic)
            .SetUpdate(true);
    }

    void OnMainButtonHoverChanged(Button button, bool isHovered)
    {
        if (currentState != MenuState.Idle)
        {
            return;
        }

        if (isHovered)
        {
            hoveredButton = button;
        }
        else if (hoveredButton == button)
        {
            hoveredButton = null;
        }

        RefreshMainButtons();
    }

    void RefreshMainButtons(bool immediate = false)
    {
        foreach (MainButtonVisual visual in mainButtons.Values)
        {
            bool isLocked = visual.button == lockedButton;
            bool isHovered = currentState == MenuState.Idle && visual.button == hoveredButton;
            bool isDisabled = !visual.button.interactable && !isLocked;
            float targetScale = isLocked || isHovered ? highlightScale : 1f;
            Color targetColor = isDisabled
                ? buttonDisabledColor
                : isLocked
                    ? buttonLockedColor
                    : isHovered
                        ? buttonHoverColor
                        : buttonNormalColor;

            ApplyMainButtonVisual(visual, targetScale, targetColor, immediate);
        }
    }

    void ApplyMainButtonVisual(MainButtonVisual visual, float targetScale, Color targetColor, bool immediate)
    {
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

    void SetMainButtonsInteractable(bool isInteractable)
    {
        SetButtonInteractable(playButton, isInteractable);
        SetButtonInteractable(creditsButton, isInteractable);
        SetButtonInteractable(optionsButton, isInteractable);
        SetButtonInteractable(quitButton, isInteractable);
    }

    void SetState(MenuState nextState, Button highlightedButton)
    {
        currentState = nextState;
        lockedButton = highlightedButton;
        hoveredButton = null;
        SetMainButtonsInteractable(nextState == MenuState.Idle);
        RefreshMainButtons();
    }

    // Hai ham ben duoi mo panel phu va khoa tam cac nut menu chinh
    // de nguoi choi chi tap trung vao panel dang mo.
    void OpenOptionsPanel()
    {
        if (currentState != MenuState.Idle)
        {
            return;
        }

        SetState(MenuState.OptionsOpen, optionsButton);
        ShowPanel(optionsPanel, optionsPanelCanvasGroup, optionsShownPosition, optionsHiddenPosition);
    }

    void OpenCreditsPanel()
    {
        if (currentState != MenuState.Idle)
        {
            return;
        }

        SetState(MenuState.CreditsOpen, creditsButton);
        ShowPanel(creditsPanel, creditsPanelCanvasGroup, creditsShownPosition, creditsHiddenPosition);
    }

    void CloseOptionsPanel()
    {
        if (currentState != MenuState.OptionsOpen)
        {
            return;
        }

        HidePanel(optionsPanel, optionsPanelCanvasGroup, optionsShownPosition, optionsHiddenPosition, () =>
        {
            SetState(MenuState.Idle, null);
            StartCoroutine(SelectButtonNextFrame(optionsButton));
        });
    }

    void CloseCreditsPanel()
    {
        if (currentState != MenuState.CreditsOpen)
        {
            return;
        }

        HidePanel(creditsPanel, creditsPanelCanvasGroup, creditsShownPosition, creditsHiddenPosition, () =>
        {
            SetState(MenuState.Idle, null);
            StartCoroutine(SelectButtonNextFrame(creditsButton));
        });
    }

    // Bam Play = luu setting hien tai, khoa UI, tat nhac menu va chuyen scene.
    void OnPlayPressed()
    {
        if (currentState != MenuState.Idle || LoadingManager.IsLoading)
        {
            return;
        }

        MenuSettingsService.Save(currentSettings);
        SetState(MenuState.Loading, playButton);
        StopMenuMusic();

        if (!LoadingManager.LoadScene(gameplaySceneName))
        {
            SetState(MenuState.Idle, null);
            Play();
            StartCoroutine(SelectButtonNextFrame(playButton));
        }
    }

    void OnQualityDecreasePressed()
    {
        ShiftQuality(-1);
    }

    void OnQualityIncreasePressed()
    {
        ShiftQuality(1);
    }

    void OnVolumeChanged(float value)
    {
        currentSettings.masterVolume = value;
        MenuSettingsService.Save(currentSettings);
        RefreshSettingsUi();
    }

    void OnSensitivityChanged(float value)
    {
        currentSettings.lookSensitivity = value;
        MenuSettingsService.Save(currentSettings);
        RefreshSettingsUi();
    }

    void ToggleDisplayMode()
    {
        currentSettings.fullscreen = !currentSettings.fullscreen;
        MenuSettingsService.Save(currentSettings);
        RefreshSettingsUi();
    }

    void ShiftQuality(int direction)
    {
        int qualityCount = Mathf.Max(1, QualitySettings.names.Length);
        currentSettings.qualityPreset = (currentSettings.qualityPreset + direction + qualityCount) % qualityCount;
        MenuSettingsService.Save(currentSettings);
        RefreshSettingsUi();
    }

    void RefreshSettingsUi()
    {
        // Day la buoc "do du lieu len UI":
        // setting hien tai trong bo nho se duoc day vao slider va text.
        if (volumeSlider != null && !Mathf.Approximately(volumeSlider.value, currentSettings.masterVolume))
        {
            volumeSlider.SetValueWithoutNotify(currentSettings.masterVolume);
        }

        if (sensitivitySlider != null && !Mathf.Approximately(sensitivitySlider.value, currentSettings.lookSensitivity))
        {
            sensitivitySlider.SetValueWithoutNotify(currentSettings.lookSensitivity);
        }

        if (volumeValueText != null)
        {
            volumeValueText.text = Mathf.RoundToInt(currentSettings.masterVolume * 100f) + "%";
        }

        if (sensitivityValueText != null)
        {
            sensitivityValueText.text = currentSettings.lookSensitivity.ToString("0.0");
        }

        if (displayModeValueText != null)
        {
            displayModeValueText.text = MenuSettingsService.GetDisplayModeLabel(currentSettings.fullscreen);
        }

        if (qualityValueText != null)
        {
            qualityValueText.text = MenuSettingsService.GetQualityLabel(currentSettings.qualityPreset);
        }
    }

    // Vi tri shown / hidden giup panel truot ra vao man hinh thay vi bat/tat ngay lap tuc.
    void ConfigurePanel(RectTransform panel, CanvasGroup canvasGroup, out Vector2 shownPosition, out Vector2 hiddenPosition)
    {
        shownPosition = Vector2.zero;
        hiddenPosition = Vector2.zero;

        if (panel == null)
        {
            return;
        }

        panel.anchorMin = new Vector2(1f, 0.5f);
        panel.anchorMax = new Vector2(1f, 0.5f);
        panel.pivot = new Vector2(1f, 0.5f);

        float width = panel.rect.width > 0f ? panel.rect.width : panel.sizeDelta.x;
        shownPosition = new Vector2(-panelRightInset, 0f);
        hiddenPosition = shownPosition + Vector2.right * (width + panelSlidePadding);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        panel.anchoredPosition = hiddenPosition;
        panel.gameObject.SetActive(false);
    }

    // ShowPanel va HidePanel la phan animate chinh cho menu con.
    void ShowPanel(RectTransform panel, CanvasGroup canvasGroup, Vector2 shownPosition, Vector2 hiddenPosition)
    {
        if (panel == null)
        {
            return;
        }

        KillPanelTween();

        panel.gameObject.SetActive(true);
        panel.anchoredPosition = hiddenPosition;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = true;
        }

        Sequence sequence = DOTween.Sequence().SetUpdate(true);
        sequence.Join(panel.DOAnchorPos(shownPosition, panelTweenDuration).SetEase(Ease.OutQuint));

        if (canvasGroup != null)
        {
            sequence.Join(canvasGroup.DOFade(1f, panelTweenDuration).SetEase(Ease.OutCubic));
        }

        sequence.OnComplete(() =>
        {
            if (canvasGroup != null)
            {
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            activePanelTween = null;
        });

        activePanelTween = sequence;
    }

    void HidePanel(RectTransform panel, CanvasGroup canvasGroup, Vector2 shownPosition, Vector2 hiddenPosition, Action onComplete)
    {
        if (panel == null)
        {
            onComplete?.Invoke();
            return;
        }

        KillPanelTween();

        if (canvasGroup != null)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = true;
        }

        Sequence sequence = DOTween.Sequence().SetUpdate(true);
        sequence.Join(panel.DOAnchorPos(hiddenPosition, panelTweenDuration).SetEase(Ease.InCubic));

        if (canvasGroup != null)
        {
            sequence.Join(canvasGroup.DOFade(0f, panelTweenDuration).SetEase(Ease.InCubic));
        }

        sequence.OnComplete(() =>
        {
            HidePanelImmediate(panel, canvasGroup, hiddenPosition);
            activePanelTween = null;
            onComplete?.Invoke();
        });

        activePanelTween = sequence;
    }

    void HidePanelImmediate(RectTransform panel, CanvasGroup canvasGroup, Vector2 hiddenPosition)
    {
        if (panel == null)
        {
            return;
        }

        panel.DOKill();
        panel.anchoredPosition = hiddenPosition;

        if (canvasGroup != null)
        {
            canvasGroup.DOKill();
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        panel.gameObject.SetActive(false);
    }

    void KillTweens()
    {
        introTween?.Kill();
        introTween = null;
        KillPanelTween();

        foreach (MainButtonVisual visual in mainButtons.Values)
        {
            visual.rect?.DOKill();
            visual.label?.DOKill();
        }
    }

    void KillPanelTween()
    {
        activePanelTween?.Kill();
        activePanelTween = null;
        optionsPanel?.DOKill();
        optionsPanelCanvasGroup?.DOKill();
        creditsPanel?.DOKill();
        creditsPanelCanvasGroup?.DOKill();
    }

    void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    IEnumerator SelectButtonNextFrame(Button button)
    {
        yield return null;

        if (button == null || EventSystem.current == null)
        {
            yield break;
        }

        EventSystem.current.SetSelectedGameObject(button.gameObject);
    }

    static TMP_Text ResolveLabel(Button button, TMP_Text currentLabel)
    {
        if (currentLabel != null)
        {
            return currentLabel;
        }

        if (button == null)
        {
            return null;
        }

        if (button.targetGraphic is TMP_Text targetLabel)
        {
            return targetLabel;
        }

        return button.GetComponentInChildren<TMP_Text>(true);
    }

    Button FindButton(string name)
    {
        if (uiCanvasGroup == null)
        {
            return null;
        }

        Button[] buttons = uiCanvasGroup.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i].name == name)
            {
                return buttons[i];
            }
        }

        return null;
    }

    static T FindInPanel<T>(RectTransform panel, string name) where T : Component
    {
        if (panel == null)
        {
            return null;
        }

        T[] results = panel.GetComponentsInChildren<T>(true);
        for (int i = 0; i < results.Length; i++)
        {
            if (results[i].name == name)
            {
                return results[i];
            }
        }

        return null;
    }

    static TMP_Text FindNextValueText(RectTransform panel, TMP_Text current)
    {
        if (panel == null)
        {
            return null;
        }

        TMP_Text[] values = panel.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < values.Length; i++)
        {
            if (values[i].name != "Value")
            {
                continue;
            }

            if (values[i] != current)
            {
                return values[i];
            }
        }

        return current;
    }

    static void BindButton(Button button, UnityAction callback)
    {
        if (button != null)
        {
            button.onClick.AddListener(callback);
        }
    }

    static void UnbindButton(Button button, UnityAction callback)
    {
        if (button != null)
        {
            button.onClick.RemoveListener(callback);
        }
    }

    static void BindSlider(Slider slider, UnityAction<float> callback)
    {
        if (slider != null)
        {
            slider.onValueChanged.AddListener(callback);
        }
    }

    static void UnbindSlider(Slider slider, UnityAction<float> callback)
    {
        if (slider != null)
        {
            slider.onValueChanged.RemoveListener(callback);
        }
    }

    static void SetButtonInteractable(Button button, bool isInteractable)
    {
        if (button != null)
        {
            button.interactable = isInteractable;
        }
    }

    sealed class MainButtonVisual
    {
        public Button button;
        public TMP_Text label;
        public RectTransform rect;
    }

    public void Play()
    {
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.PlayMusic("MainMenu");
        }
    }

    void StopMenuMusic()
    {
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.StopMusic();
        }
    }
}

sealed class MainMenuButtonHoverRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    Button targetButton;
    Action<Button, bool> callback;

    public void Initialize(Button button, Action<Button, bool> onHoverChanged)
    {
        targetButton = button;
        callback = onHoverChanged;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        callback?.Invoke(targetButton, true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        callback?.Invoke(targetButton, false);
    }
}
