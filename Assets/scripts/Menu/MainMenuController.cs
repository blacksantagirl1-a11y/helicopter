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
    enum MenuState
    {
        Idle,
        OptionsOpen,
        CreditsOpen,
        Loading
    }

    const string DefaultGameplaySceneName = "Suml";

    [Header("Scene Flow")]
    [SerializeField] string gameplaySceneName = DefaultGameplaySceneName;

    [Header("Canvas")]
    [SerializeField] CanvasGroup uiCanvasGroup;

    [Header("Primary Buttons")]
    [SerializeField] Button playButton;
    [SerializeField] TMP_Text playButtonLabel;
    [SerializeField] Button creditsButton;
    [SerializeField] TMP_Text creditsButtonLabel;
    [SerializeField] Button optionsButton;
    [SerializeField] TMP_Text optionsButtonLabel;
    [SerializeField] Button quitButton;
    [SerializeField] TMP_Text quitButtonLabel;

    [Header("Panels")]
    [SerializeField] RectTransform optionsPanel;
    [SerializeField] CanvasGroup optionsPanelCanvasGroup;
    [SerializeField] Button optionsBackButton;
    [SerializeField] RectTransform creditsPanel;
    [SerializeField] CanvasGroup creditsPanelCanvasGroup;
    [SerializeField] Button creditsBackButton;

    [Header("Options")]
    [SerializeField] Slider volumeSlider;
    [SerializeField] TMP_Text volumeValueText;
    [SerializeField] Slider sensitivitySlider;
    [SerializeField] TMP_Text sensitivityValueText;
    [SerializeField] Button displayModeButton;
    [SerializeField] TMP_Text displayModeValueText;
    [SerializeField] Button qualityDecreaseButton;
    [SerializeField] Button qualityIncreaseButton;
    [SerializeField] TMP_Text qualityValueText;

    [Header("Animation")]
    [SerializeField, Min(0f)] float introFadeDuration = 0.45f;
    [SerializeField, Min(0f)] float panelTweenDuration = 0.35f;
    [SerializeField, Min(0f)] float panelSlidePadding = 140f;
    [SerializeField, Min(0f)] float panelRightInset = 72f;
    [SerializeField, Min(1f)] float highlightScale = 1.06f;
    [SerializeField, Min(0f)] float buttonTweenDuration = 0.16f;

    [Header("Palette")]
    [SerializeField] Color buttonNormalColor = new(0.92f, 0.96f, 1f, 0.78f);
    [SerializeField] Color buttonHoverColor = Color.white;
    [SerializeField] Color buttonLockedColor = new(1f, 0.96f, 0.82f, 1f);
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
    }

    void Start()
    {
        StartIntroFade();
        StartCoroutine(SelectButtonNextFrame(playButton));
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

    void OnPlayPressed()
    {
        if (currentState != MenuState.Idle || LoadingManager.IsLoading)
        {
            return;
        }

        MenuSettingsService.Save(currentSettings);
        SetState(MenuState.Loading, playButton);

        if (!LoadingManager.LoadScene(gameplaySceneName))
        {
            SetState(MenuState.Idle, null);
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
