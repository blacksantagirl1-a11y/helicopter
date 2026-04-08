using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    const string GameplaySceneName = "Suml";
    const string GameTitle = "SUML";
    const string MenuVersionLabel = "Prototype Build";
    const string CreditsBody =
        "Developed by\nYour Team Name\n\n" +
        "Gameplay\nEnvironment\nUI\n\n" +
        "Built with Unity\n\n" +
        "Third-party assets\nUpdate this section before release.";

    static readonly Color ScreenTint = new(0.02f, 0.03f, 0.04f, 0.24f);
    static readonly Color LeftTint = new(0.01f, 0.015f, 0.02f, 0.54f);
    static readonly Color MidShadeTint = new(0.01f, 0.015f, 0.02f, 0.22f);
    static readonly Color PanelTint = new(0.03f, 0.04f, 0.05f, 0.92f);
    static readonly Color OverlayTint = new(0f, 0f, 0f, 0.72f);
    static readonly Color MainButtonNormal = new(0.92f, 0.96f, 1f, 0.78f);
    static readonly Color MainButtonHover = new(1f, 1f, 1f, 1f);
    static readonly Color MainButtonSelected = new(1f, 0.96f, 0.82f, 1f);
    static readonly Color PanelButtonColor = new(0.14f, 0.17f, 0.2f, 0.96f);
    static readonly Color SliderTrackColor = new(0.12f, 0.14f, 0.16f, 0.95f);
    static readonly Color SliderFillColor = new(0.81f, 0.89f, 0.96f, 1f);
    static readonly Color TextPrimary = new(0.94f, 0.97f, 1f, 1f);
    static readonly Color TextMuted = new(0.76f, 0.8f, 0.86f, 1f);

    TMP_FontAsset menuFont;
    CanvasGroup uiCanvasGroup;
    Image fadeOverlay;
    GameObject modalOverlay;
    GameObject optionsPanel;
    GameObject creditsPanel;
    GameObject quitPanel;
    Slider volumeSlider;
    Slider sensitivitySlider;
    TMP_Text volumeValueText;
    TMP_Text sensitivityValueText;
    TMP_Text displayModeValueText;
    TMP_Text qualityValueText;
    TMP_Text playButtonLabel;
    Button playButton;
    Button optionsButton;
    Button creditsButton;
    Button quitButton;
    Button lastMenuButton;
    MenuSettingsData currentSettings;
    bool isLoadingScene;

    void Awake()
    {
        currentSettings = MenuSettingsService.Load();
        menuFont = ResolveFont();
        UnlockCursor();
        EnsureEventSystem();
        BuildMenu();
        RefreshSettingsUi();
    }

    void Start()
    {
        StartCoroutine(FadeInRoutine());
        StartCoroutine(SelectButtonNextFrame(playButton));
    }

    void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            return;
        }

        if (isLoadingScene)
        {
            return;
        }

        if (quitPanel.activeSelf)
        {
            ClosePanels();
            return;
        }

        if (optionsPanel.activeSelf || creditsPanel.activeSelf)
        {
            ClosePanels();
            return;
        }

        OpenQuitPanel();
    }

    void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemObject = new("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        eventSystemObject.transform.SetParent(transform, false);
        eventSystemObject.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
    }

    void BuildMenu()
    {
        GameObject canvasObject = new("MainMenuCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        uiCanvasGroup = canvasObject.GetComponent<CanvasGroup>();
        uiCanvasGroup.alpha = 0f;

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        CreateStretchImage("ScreenTint", canvasRect, ScreenTint);

        Image leftShade = CreateStretchImage("LeftShade", canvasRect, LeftTint);
        RectTransform leftShadeRect = leftShade.rectTransform;
        leftShadeRect.anchorMin = new Vector2(0f, 0f);
        leftShadeRect.anchorMax = new Vector2(0.38f, 1f);
        leftShadeRect.offsetMin = Vector2.zero;
        leftShadeRect.offsetMax = Vector2.zero;

        Image midShade = CreateStretchImage("MidShade", canvasRect, MidShadeTint);
        RectTransform midShadeRect = midShade.rectTransform;
        midShadeRect.anchorMin = new Vector2(0f, 0f);
        midShadeRect.anchorMax = new Vector2(0.58f, 1f);
        midShadeRect.offsetMin = Vector2.zero;
        midShadeRect.offsetMax = Vector2.zero;

        leftShade.transform.SetAsLastSibling();
        midShade.transform.SetSiblingIndex(leftShade.transform.GetSiblingIndex() - 1);
        leftShadeRect.offsetMin = Vector2.zero;
        leftShadeRect.offsetMax = Vector2.zero;

        RectTransform titleRoot = CreateRectTransform("TitleRoot", canvasRect);
        titleRoot.anchorMin = new Vector2(0f, 1f);
        titleRoot.anchorMax = new Vector2(0f, 1f);
        titleRoot.pivot = new Vector2(0f, 1f);
        titleRoot.anchoredPosition = new Vector2(88f, -70f);
        titleRoot.sizeDelta = new Vector2(460f, 120f);

        CreateText("GameTitle", titleRoot, GameTitle, 68f, FontStyles.Bold, TextAlignmentOptions.TopLeft, TextPrimary);
        TMP_Text versionLabel = CreateText("Version", titleRoot, MenuVersionLabel, 20f, FontStyles.Normal, TextAlignmentOptions.BottomLeft, TextMuted);
        versionLabel.rectTransform.anchorMin = new Vector2(0f, 0f);
        versionLabel.rectTransform.anchorMax = new Vector2(1f, 0f);
        versionLabel.rectTransform.pivot = new Vector2(0f, 0f);
        versionLabel.rectTransform.anchoredPosition = new Vector2(2f, -38f);

        RectTransform menuRoot = CreateRectTransform("MenuRoot", canvasRect);
        menuRoot.anchorMin = new Vector2(0f, 0.5f);
        menuRoot.anchorMax = new Vector2(0f, 0.5f);
        menuRoot.pivot = new Vector2(0f, 0.5f);
        menuRoot.anchoredPosition = new Vector2(78f, 18f);
        menuRoot.sizeDelta = new Vector2(420f, 420f);

        VerticalLayoutGroup menuLayout = menuRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        menuLayout.childAlignment = TextAnchor.MiddleLeft;
        menuLayout.childControlWidth = true;
        menuLayout.childControlHeight = true;
        menuLayout.childForceExpandWidth = true;
        menuLayout.childForceExpandHeight = false;
        menuLayout.spacing = 12f;

        playButton = CreateMainMenuButton(menuRoot, "PLAY", OnPlayPressed, out playButtonLabel);
        optionsButton = CreateMainMenuButton(menuRoot, "OPTIONS", OpenOptionsPanel, out _);
        creditsButton = CreateMainMenuButton(menuRoot, "CREDITS", OpenCreditsPanel, out _);
        quitButton = CreateMainMenuButton(menuRoot, "QUIT", OpenQuitPanel, out _);

        TMP_Text footerText = CreateText("Footer", canvasRect, "Mouse free in menu. Press Esc to close open panels.", 18f, FontStyles.Normal, TextAlignmentOptions.BottomLeft, TextMuted);
        footerText.rectTransform.anchorMin = new Vector2(0f, 0f);
        footerText.rectTransform.anchorMax = new Vector2(0f, 0f);
        footerText.rectTransform.pivot = new Vector2(0f, 0f);
        footerText.rectTransform.anchoredPosition = new Vector2(88f, 36f);
        footerText.rectTransform.sizeDelta = new Vector2(540f, 24f);

        modalOverlay = CreateStretchImage("ModalOverlay", canvasRect, OverlayTint).gameObject;
        modalOverlay.SetActive(false);

        optionsPanel = CreateModalPanel(modalOverlay.transform, "OptionsPanel", "OPTIONS", new Vector2(700f, 470f));
        BuildOptionsPanel(optionsPanel.transform);
        optionsPanel.SetActive(false);

        creditsPanel = CreateModalPanel(modalOverlay.transform, "CreditsPanel", "CREDITS", new Vector2(620f, 420f));
        BuildCreditsPanel(creditsPanel.transform);
        creditsPanel.SetActive(false);

        quitPanel = CreateModalPanel(modalOverlay.transform, "QuitPanel", "QUIT GAME", new Vector2(500f, 280f));
        BuildQuitPanel(quitPanel.transform);
        quitPanel.SetActive(false);

        fadeOverlay = CreateStretchImage("FadeOverlay", canvasRect, Color.black);
        fadeOverlay.raycastTarget = false;
    }

    void BuildOptionsPanel(Transform panelTransform)
    {
        RectTransform contentRoot = CreateContentRoot(panelTransform);
        CreateSliderRow(contentRoot, "Master Volume", 0f, 1f, currentSettings.masterVolume, OnVolumeChanged, out volumeSlider, out volumeValueText);
        CreateSliderRow(contentRoot, "Look Sensitivity", 0.5f, 8f, currentSettings.lookSensitivity, OnSensitivityChanged, out sensitivitySlider, out sensitivityValueText);

        RectTransform displayRow = CreateRow(contentRoot, 62f);
        CreateLabel(displayRow, "Display Mode");
        CreatePanelButton(displayRow, MenuSettingsService.GetDisplayModeLabel(currentSettings.fullscreen), ToggleDisplayMode, out displayModeValueText, new Vector2(220f, 44f));

        RectTransform qualityRow = CreateRow(contentRoot, 62f);
        CreateLabel(qualityRow, "Quality");

        RectTransform qualityControls = CreateRectTransform("QualityControls", qualityRow);
        LayoutElement qualityControlsLayout = qualityControls.gameObject.AddComponent<LayoutElement>();
        qualityControlsLayout.flexibleWidth = 1f;

        HorizontalLayoutGroup qualityLayout = qualityControls.gameObject.AddComponent<HorizontalLayoutGroup>();
        qualityLayout.childAlignment = TextAnchor.MiddleRight;
        qualityLayout.childControlWidth = true;
        qualityLayout.childControlHeight = true;
        qualityLayout.childForceExpandWidth = false;
        qualityLayout.childForceExpandHeight = false;
        qualityLayout.spacing = 12f;

        CreatePanelButton(qualityControls, "-", () => ShiftQuality(-1), out _, new Vector2(44f, 44f));
        TMP_Text qualityLabel = CreateText("QualityValue", qualityControls, MenuSettingsService.GetQualityLabel(currentSettings.qualityPreset), 26f, FontStyles.Bold, TextAlignmentOptions.Midline, TextPrimary);
        LayoutElement qualityLabelLayout = qualityLabel.gameObject.AddComponent<LayoutElement>();
        qualityLabelLayout.preferredWidth = 220f;
        qualityLabelLayout.minWidth = 220f;
        qualityValueText = qualityLabel;
        CreatePanelButton(qualityControls, "+", () => ShiftQuality(1), out _, new Vector2(44f, 44f));

        RectTransform buttonRow = CreateRectTransform("Actions", contentRoot);
        HorizontalLayoutGroup buttonRowLayout = buttonRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        buttonRowLayout.childAlignment = TextAnchor.MiddleRight;
        buttonRowLayout.childControlWidth = true;
        buttonRowLayout.childControlHeight = true;
        buttonRowLayout.childForceExpandWidth = false;
        buttonRowLayout.childForceExpandHeight = false;
        buttonRowLayout.spacing = 14f;
        CreatePanelButton(buttonRow, "Close", ClosePanels, out _, new Vector2(160f, 48f));
    }

    void BuildCreditsPanel(Transform panelTransform)
    {
        RectTransform contentRoot = CreateContentRoot(panelTransform);
        TMP_Text creditsText = CreateText("CreditsBody", contentRoot, CreditsBody, 27f, FontStyles.Normal, TextAlignmentOptions.TopLeft, TextPrimary);
        creditsText.enableWordWrapping = true;
        LayoutElement creditsLayout = creditsText.gameObject.AddComponent<LayoutElement>();
        creditsLayout.flexibleHeight = 1f;
        creditsLayout.minHeight = 220f;

        RectTransform buttonRow = CreateRectTransform("Actions", contentRoot);
        HorizontalLayoutGroup buttonRowLayout = buttonRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        buttonRowLayout.childAlignment = TextAnchor.MiddleRight;
        buttonRowLayout.childControlWidth = true;
        buttonRowLayout.childControlHeight = true;
        buttonRowLayout.childForceExpandWidth = false;
        buttonRowLayout.childForceExpandHeight = false;
        buttonRowLayout.spacing = 14f;
        CreatePanelButton(buttonRow, "Back", ClosePanels, out _, new Vector2(160f, 48f));
    }

    void BuildQuitPanel(Transform panelTransform)
    {
        RectTransform contentRoot = CreateContentRoot(panelTransform);
        TMP_Text body = CreateText("QuitBody", contentRoot, "Return to desktop now?", 30f, FontStyles.Normal, TextAlignmentOptions.Left, TextPrimary);
        LayoutElement bodyLayout = body.gameObject.AddComponent<LayoutElement>();
        bodyLayout.flexibleHeight = 1f;
        bodyLayout.minHeight = 110f;

        RectTransform buttonRow = CreateRectTransform("Actions", contentRoot);
        HorizontalLayoutGroup buttonRowLayout = buttonRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        buttonRowLayout.childAlignment = TextAnchor.MiddleRight;
        buttonRowLayout.childControlWidth = true;
        buttonRowLayout.childControlHeight = true;
        buttonRowLayout.childForceExpandWidth = false;
        buttonRowLayout.childForceExpandHeight = false;
        buttonRowLayout.spacing = 14f;
        CreatePanelButton(buttonRow, "Stay", ClosePanels, out _, new Vector2(140f, 48f));
        CreatePanelButton(buttonRow, "Quit", QuitGame, out _, new Vector2(140f, 48f));
    }

    GameObject CreateModalPanel(Transform parent, string name, string title, Vector2 size)
    {
        RectTransform panel = CreateRectTransform(name, parent);
        panel.anchorMin = new Vector2(0.5f, 0.5f);
        panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.sizeDelta = size;

        Image panelImage = panel.gameObject.AddComponent<Image>();
        panelImage.color = PanelTint;

        Shadow shadow = panel.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
        shadow.effectDistance = new Vector2(0f, -10f);

        TMP_Text titleText = CreateText("Title", panel, title, 34f, FontStyles.Bold, TextAlignmentOptions.TopLeft, TextPrimary);
        titleText.rectTransform.anchorMin = new Vector2(0f, 1f);
        titleText.rectTransform.anchorMax = new Vector2(1f, 1f);
        titleText.rectTransform.pivot = new Vector2(0f, 1f);
        titleText.rectTransform.offsetMin = new Vector2(32f, -60f);
        titleText.rectTransform.offsetMax = new Vector2(-32f, -20f);

        Image divider = CreateStretchImage("Divider", panel, new Color(1f, 1f, 1f, 0.08f));
        RectTransform dividerRect = divider.rectTransform;
        dividerRect.anchorMin = new Vector2(0f, 1f);
        dividerRect.anchorMax = new Vector2(1f, 1f);
        dividerRect.pivot = new Vector2(0.5f, 1f);
        dividerRect.offsetMin = new Vector2(30f, -76f);
        dividerRect.offsetMax = new Vector2(-30f, -74f);

        return panel.gameObject;
    }

    RectTransform CreateContentRoot(Transform panelTransform)
    {
        RectTransform contentRoot = CreateRectTransform("Content", panelTransform);
        contentRoot.anchorMin = new Vector2(0f, 0f);
        contentRoot.anchorMax = new Vector2(1f, 1f);
        contentRoot.offsetMin = new Vector2(30f, 28f);
        contentRoot.offsetMax = new Vector2(-30f, -88f);

        VerticalLayoutGroup layout = contentRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.spacing = 18f;

        return contentRoot;
    }

    RectTransform CreateRow(Transform parent, float height)
    {
        RectTransform row = CreateRectTransform("Row", parent);
        LayoutElement layoutElement = row.gameObject.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = height;

        HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.spacing = 18f;

        return row;
    }

    void CreateSliderRow(Transform parent, string label, float minValue, float maxValue, float initialValue, UnityAction<float> onValueChanged, out Slider slider, out TMP_Text valueLabel)
    {
        RectTransform row = CreateRow(parent, 68f);
        CreateLabel(row, label);

        RectTransform sliderRoot = CreateRectTransform("SliderRoot", row);
        LayoutElement sliderLayout = sliderRoot.gameObject.AddComponent<LayoutElement>();
        sliderLayout.flexibleWidth = 1f;
        sliderLayout.minWidth = 220f;

        slider = CreateSlider(sliderRoot);
        slider.minValue = minValue;
        slider.maxValue = maxValue;
        slider.value = initialValue;
        slider.onValueChanged.AddListener(onValueChanged);

        valueLabel = CreateText("Value", row, string.Empty, 22f, FontStyles.Bold, TextAlignmentOptions.Right, TextPrimary);
        LayoutElement valueLayout = valueLabel.gameObject.AddComponent<LayoutElement>();
        valueLayout.preferredWidth = 86f;
        valueLayout.minWidth = 86f;
    }

    TMP_Text CreateLabel(Transform parent, string label)
    {
        TMP_Text text = CreateText(label + "Label", parent, label, 24f, FontStyles.Normal, TextAlignmentOptions.Left, TextPrimary);
        LayoutElement layout = text.gameObject.AddComponent<LayoutElement>();
        layout.preferredWidth = 180f;
        layout.minWidth = 180f;
        return text;
    }

    Slider CreateSlider(Transform parent)
    {
        RectTransform sliderRoot = CreateRectTransform("Slider", parent);
        sliderRoot.sizeDelta = new Vector2(0f, 24f);

        Slider slider = sliderRoot.gameObject.AddComponent<Slider>();
        Image background = CreateStretchImage("Background", sliderRoot, SliderTrackColor);
        background.rectTransform.offsetMin = new Vector2(0f, 4f);
        background.rectTransform.offsetMax = new Vector2(0f, -4f);

        RectTransform fillArea = CreateRectTransform("FillArea", sliderRoot);
        fillArea.anchorMin = new Vector2(0f, 0f);
        fillArea.anchorMax = new Vector2(1f, 1f);
        fillArea.offsetMin = new Vector2(6f, 4f);
        fillArea.offsetMax = new Vector2(-16f, -4f);

        Image fill = CreateStretchImage("Fill", fillArea, SliderFillColor);
        fill.rectTransform.offsetMin = Vector2.zero;
        fill.rectTransform.offsetMax = Vector2.zero;

        RectTransform handleSlideArea = CreateRectTransform("HandleSlideArea", sliderRoot);
        handleSlideArea.anchorMin = new Vector2(0f, 0f);
        handleSlideArea.anchorMax = new Vector2(1f, 1f);
        handleSlideArea.offsetMin = new Vector2(10f, 0f);
        handleSlideArea.offsetMax = new Vector2(-10f, 0f);

        Image handle = CreateImage("Handle", handleSlideArea, TextPrimary);
        RectTransform handleRect = handle.rectTransform;
        handleRect.sizeDelta = new Vector2(18f, 32f);
        handleRect.anchorMin = new Vector2(0.5f, 0.5f);
        handleRect.anchorMax = new Vector2(0.5f, 0.5f);
        handleRect.pivot = new Vector2(0.5f, 0.5f);

        slider.targetGraphic = handle;
        slider.fillRect = fill.rectTransform;
        slider.handleRect = handleRect;
        slider.direction = Slider.Direction.LeftToRight;
        return slider;
    }

    Button CreateMainMenuButton(Transform parent, string label, UnityAction onClick, out TMP_Text labelText)
    {
        RectTransform buttonRect = CreateRectTransform(label + "Button", parent);
        LayoutElement layoutElement = buttonRect.gameObject.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 84f;

        Button button = buttonRect.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.ColorTint;
        button.onClick.AddListener(onClick);

        labelText = CreateText("Label", buttonRect, label, 72f, FontStyles.Bold, TextAlignmentOptions.Left, MainButtonNormal);
        labelText.enableAutoSizing = true;
        labelText.fontSizeMin = 42f;
        labelText.fontSizeMax = 72f;
        labelText.raycastTarget = true;

        RectTransform labelRect = labelText.rectTransform;
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        button.targetGraphic = labelText;

        ColorBlock colors = button.colors;
        colors.normalColor = MainButtonNormal;
        colors.highlightedColor = MainButtonHover;
        colors.pressedColor = MainButtonSelected;
        colors.selectedColor = MainButtonSelected;
        colors.disabledColor = new Color(1f, 1f, 1f, 0.35f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.12f;
        button.colors = colors;

        return button;
    }

    Button CreatePanelButton(Transform parent, string label, UnityAction onClick, out TMP_Text labelText, Vector2 size)
    {
        RectTransform buttonRect = CreateRectTransform(label + "Button", parent);
        LayoutElement layoutElement = buttonRect.gameObject.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = size.x;
        layoutElement.preferredHeight = size.y;
        layoutElement.minWidth = size.x;
        layoutElement.minHeight = size.y;

        Image image = buttonRect.gameObject.AddComponent<Image>();
        image.color = PanelButtonColor;

        Button button = buttonRect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);

        ColorBlock colors = button.colors;
        colors.normalColor = PanelButtonColor;
        colors.highlightedColor = new Color(0.2f, 0.24f, 0.28f, 1f);
        colors.pressedColor = new Color(0.12f, 0.15f, 0.18f, 1f);
        colors.selectedColor = new Color(0.2f, 0.24f, 0.28f, 1f);
        colors.disabledColor = new Color(0.1f, 0.1f, 0.1f, 0.5f);
        colors.fadeDuration = 0.12f;
        button.colors = colors;

        labelText = CreateText("Label", buttonRect, label, 22f, FontStyles.Bold, TextAlignmentOptions.Center, TextPrimary);
        RectTransform labelRect = labelText.rectTransform;
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        labelText.raycastTarget = false;

        return button;
    }

    RectTransform CreateRectTransform(string name, Transform parent)
    {
        GameObject gameObject = new(name, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        return gameObject.GetComponent<RectTransform>();
    }

    Image CreateStretchImage(string name, Transform parent, Color color)
    {
        Image image = CreateImage(name, parent, color);
        RectTransform rect = image.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return image;
    }

    Image CreateImage(string name, Transform parent, Color color)
    {
        RectTransform rect = CreateRectTransform(name, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        return image;
    }

    TMP_Text CreateText(string name, Transform parent, string textValue, float fontSize, FontStyles fontStyle, TextAlignmentOptions alignment, Color color)
    {
        RectTransform rect = CreateRectTransform(name, parent);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = textValue;
        text.font = menuFont;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    TMP_FontAsset ResolveFont()
    {
        if (TMP_Settings.defaultFontAsset != null)
        {
            return TMP_Settings.defaultFontAsset;
        }

        TMP_FontAsset fallback = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        return fallback != null ? fallback : TMP_Settings.defaultFontAsset;
    }

    void OnPlayPressed()
    {
        if (isLoadingScene)
        {
            return;
        }

        isLoadingScene = true;
        playButton.interactable = false;
        playButtonLabel.text = "LOADING...";
        MenuSettingsService.Save(currentSettings);
        SceneManager.LoadScene(GameplaySceneName);
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

    void OpenOptionsPanel()
    {
        lastMenuButton = optionsButton;
        SetActivePanel(optionsPanel);
    }

    void OpenCreditsPanel()
    {
        lastMenuButton = creditsButton;
        SetActivePanel(creditsPanel);
    }

    void OpenQuitPanel()
    {
        lastMenuButton = quitButton;
        SetActivePanel(quitPanel);
    }

    void ClosePanels()
    {
        modalOverlay.SetActive(false);
        optionsPanel.SetActive(false);
        creditsPanel.SetActive(false);
        quitPanel.SetActive(false);
        StartCoroutine(SelectButtonNextFrame(lastMenuButton == null ? playButton : lastMenuButton));
    }

    void SetActivePanel(GameObject activePanel)
    {
        modalOverlay.SetActive(true);
        optionsPanel.SetActive(activePanel == optionsPanel);
        creditsPanel.SetActive(activePanel == creditsPanel);
        quitPanel.SetActive(activePanel == quitPanel);
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

    IEnumerator FadeInRoutine()
    {
        const float duration = 0.6f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            uiCanvasGroup.alpha = t;

            if (fadeOverlay != null)
            {
                Color color = fadeOverlay.color;
                color.a = 1f - t;
                fadeOverlay.color = color;
            }

            yield return null;
        }

        uiCanvasGroup.alpha = 1f;
        if (fadeOverlay != null)
        {
            fadeOverlay.gameObject.SetActive(false);
        }
    }

    IEnumerator SelectButtonNextFrame(Button button)
    {
        yield return null;

        if (button != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(button.gameObject);
        }
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
}
