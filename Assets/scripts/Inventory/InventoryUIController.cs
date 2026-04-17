using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class InventoryUIController : MonoBehaviour
{
    private sealed class SlotView
    {
        public Button Button;
        public Image Background;
        public Image Icon;
        public TextMeshProUGUI AmountLabel;
        public TextMeshProUGUI PlaceholderLabel;
    }

    [Header("Input")]
    [SerializeField] private KeyCode toggleInventoryKey = KeyCode.B;

    [Header("References")]
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private PlayerUI playerUI;
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private Volume blurVolume;

    [Header("Layout")]
    [SerializeField]
    [Min(1)]
    private int columns = 5;
    [SerializeField] private Vector2 slotSize = new Vector2(60f, 60f);
    [SerializeField] private Vector2 slotSpacing = new Vector2(8f, 8f);

    [Header("Look")]
    [SerializeField] private Color backdropColor = new Color(0.02f, 0.17f, 0.20f, 0.58f);
    [SerializeField] private Color panelColor = new Color(0.03f, 0.14f, 0.18f, 0.80f);
    [SerializeField] private Color slotEmptyColor = new Color(0.06f, 0.20f, 0.24f, 0.78f);
    [SerializeField] private Color slotFilledColor = new Color(0.13f, 0.28f, 0.32f, 0.96f);
    [SerializeField] private Color slotOutlineColor = new Color(0.21f, 0.68f, 0.77f, 0.56f);
    [SerializeField] private Color amountColor = new Color(1f, 0.42f, 0.34f, 1f);
    [SerializeField] private Color placeholderColor = new Color(0.82f, 0.95f, 0.96f, 0.95f);

    [Header("Blur")]
    [SerializeField] private float blurGaussianStart = 0.1f;
    [SerializeField] private float blurGaussianEnd = 4f;
    [SerializeField] private float blurRadius = 1f;

    private readonly List<SlotView> slotViews = new List<SlotView>();
    private readonly Dictionary<Behaviour, bool> cachedControlStates = new Dictionary<Behaviour, bool>();

    private GameObject inventoryRoot;
    private CanvasGroup inventoryCanvasGroup;
    private TextMeshProUGUI statusLabel;
    private RectTransform slotGridRoot;

    private PlayerMovement playerMovement;
    private Jump jump;
    private PlayerLook playerLook;
    private Zoom zoom;
    private PickUpScript pickUpScript;
    private ActionScript actionScript;
    private CuttingTreeSystem cuttingTreeSystem;
    private Rigidbody playerRigidbody;

    private DepthOfField blurEffect;
    private bool blurStateCaptured;
    private bool blurOriginalActive;
    private DepthOfFieldMode blurOriginalMode;
    private float blurOriginalStart;
    private float blurOriginalEnd;
    private float blurOriginalRadius;

    private bool isInventoryOpen;

    public bool IsInventoryOpen => isInventoryOpen;

    private void Reset()
    {
        TryAutoAssignReferences();
        EnsureInventoryUI();
        RefreshSlots();
        SetInventoryVisible(false);
    }

    private void Awake()
    {
        TryAutoAssignReferences();
        EnsureInventoryUI();
        RefreshSlots();
        SetInventoryVisible(false);
    }

    private void OnEnable()
    {
        TryAutoAssignReferences();

        if (playerInventory != null)
        {
            playerInventory.InventoryChanged += RefreshSlots;
            playerInventory.FeedbackRequested += HandleInventoryFeedback;
        }
    }

    private void OnDisable()
    {
        if (playerInventory != null)
        {
            playerInventory.InventoryChanged -= RefreshSlots;
            playerInventory.FeedbackRequested -= HandleInventoryFeedback;
        }

        RestoreControls();
        SetBlurActive(false);
    }

    private void OnValidate()
    {
        TryAutoAssignReferences();
        columns = Mathf.Max(1, columns);
        slotSize.x = Mathf.Max(48f, slotSize.x);
        slotSize.y = Mathf.Max(48f, slotSize.y);
        slotSpacing.x = Mathf.Max(0f, slotSpacing.x);
        slotSpacing.y = Mathf.Max(0f, slotSpacing.y);
        blurRadius = Mathf.Clamp(blurRadius, 0.1f, 1f);
        blurGaussianEnd = Mathf.Max(blurGaussianStart + 0.1f, blurGaussianEnd);
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleInventoryKey))
        {
            ToggleInventory();
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            return;
        }

        if (isInventoryOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void ToggleInventory()
    {
        SetInventoryOpen(!isInventoryOpen);
    }

    public void SetInventoryOpen(bool shouldOpen)
    {
        if (isInventoryOpen == shouldOpen)
        {
            return;
        }

        isInventoryOpen = shouldOpen;
        EnsureInventoryUI();
        SetInventoryVisible(shouldOpen);

        if (shouldOpen)
        {
            CacheAndDisableControls();
            SetBlurActive(true);
            RefreshSlots();

            if (playerUI != null)
            {
                playerUI.UpdatePrompt(string.Empty);
                playerUI.HideInteractionContent();
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        RestoreControls();
        SetBlurActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void TryAutoAssignReferences()
    {
        playerInventory ??= GetComponent<PlayerInventory>();
        playerInventory ??= FindFirstObjectByType<PlayerInventory>();

        playerUI ??= GetComponent<PlayerUI>();
        playerUI ??= FindFirstObjectByType<PlayerUI>();

        if (targetCanvas == null && playerUI != null && playerUI.PickUpText != null)
        {
            targetCanvas = playerUI.PickUpText.canvas;
        }

        targetCanvas ??= FindFirstObjectByType<Canvas>();
        blurVolume ??= FindBestBlurVolume();

        playerMovement ??= GetComponent<PlayerMovement>();
        jump ??= GetComponent<Jump>();
        actionScript ??= GetComponent<ActionScript>();
        playerRigidbody ??= GetComponent<Rigidbody>();

        if (playerLook == null || zoom == null || pickUpScript == null || cuttingTreeSystem == null)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                playerLook ??= mainCamera.GetComponent<PlayerLook>();
                zoom ??= mainCamera.GetComponent<Zoom>();
                pickUpScript ??= mainCamera.GetComponent<PickUpScript>();
                cuttingTreeSystem ??= mainCamera.GetComponent<CuttingTreeSystem>();
            }
        }
    }

    private void EnsureInventoryUI()
    {
        if (targetCanvas == null)
        {
            TryAutoAssignReferences();
            if (targetCanvas == null)
            {
                return;
            }
        }

        if (inventoryRoot != null && inventoryCanvasGroup != null && slotGridRoot != null)
        {
            if (playerInventory != null && slotViews.Count != playerInventory.SlotCount)
            {
                RebuildSlotGrid();
            }

            return;
        }

        Transform existingRoot = targetCanvas.transform.Find("InventoryRoot");
        if (existingRoot != null)
        {
            inventoryRoot = existingRoot.gameObject;
            inventoryCanvasGroup = inventoryRoot.GetComponent<CanvasGroup>();
            slotGridRoot = inventoryRoot.transform.Find("InventoryPanel/SlotGrid") as RectTransform;
            statusLabel = FindText(inventoryRoot.transform, "InventoryPanel/FooterLabel");

            if (slotGridRoot != null)
            {
                RebuildSlotGrid();
                return;
            }
        }

        inventoryRoot = new GameObject("InventoryRoot", typeof(RectTransform), typeof(CanvasGroup));
        inventoryRoot.transform.SetParent(targetCanvas.transform, false);
        inventoryCanvasGroup = inventoryRoot.GetComponent<CanvasGroup>();
        StretchToParent(inventoryRoot.GetComponent<RectTransform>());

        CreateInventoryUI();
    }

    private void CreateInventoryUI()
    {
        slotViews.Clear();

        Image backdrop = CreateImage(
            "InventoryBackdrop",
            inventoryRoot.transform,
            backdropColor,
            stretchToParent: true);
        backdrop.raycastTarget = true;

        RectTransform panelRect = CreatePanel();
        CreateHeader(panelRect);
        slotGridRoot = CreateSlotGrid(panelRect);
        statusLabel = CreateFooter(panelRect);

        RebuildSlotGrid();
    }

    private RectTransform CreatePanel()
    {
        Image panelImage = CreateImage("InventoryPanel", inventoryRoot.transform, panelColor, stretchToParent: false);
        panelImage.raycastTarget = true;

        RectTransform panelRect = panelImage.rectTransform;
        panelRect.anchorMin = new Vector2(0f, 0.5f);
        panelRect.anchorMax = new Vector2(0f, 0.5f);
        panelRect.pivot = new Vector2(0f, 0.5f);
        panelRect.anchoredPosition = new Vector2(30f, 0f);
        panelRect.sizeDelta = new Vector2(364f, 430f);

        Outline panelOutline = panelImage.gameObject.AddComponent<Outline>();
        panelOutline.effectColor = new Color(0.15f, 0.72f, 0.82f, 0.35f);
        panelOutline.effectDistance = new Vector2(1f, -1f);

        return panelRect;
    }

    private void CreateHeader(RectTransform panelRect)
    {
        TextMeshProUGUI title = CreateText(
            "Title",
            panelRect,
            "Túi đồ",
            28f,
            FontStyles.Bold,
            TextAlignmentOptions.Left);
        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(0f, 1f);
        titleRect.pivot = new Vector2(0f, 1f);
        titleRect.anchoredPosition = new Vector2(18f, -16f);
        titleRect.sizeDelta = new Vector2(180f, 36f);
        title.color = new Color(0.91f, 0.98f, 1f, 1f);

        TextMeshProUGUI closeHint = CreateText(
            "CloseHint",
            panelRect,
            "B để đóng",
            16f,
            FontStyles.Normal,
            TextAlignmentOptions.Right);
        RectTransform hintRect = closeHint.rectTransform;
        hintRect.anchorMin = new Vector2(1f, 1f);
        hintRect.anchorMax = new Vector2(1f, 1f);
        hintRect.pivot = new Vector2(1f, 1f);
        hintRect.anchoredPosition = new Vector2(-18f, -18f);
        hintRect.sizeDelta = new Vector2(160f, 26f);
        closeHint.color = new Color(0.75f, 0.90f, 0.94f, 0.95f);
    }

    private RectTransform CreateSlotGrid(RectTransform panelRect)
    {
        GameObject gridObject = new GameObject(
            "SlotGrid",
            typeof(RectTransform),
            typeof(GridLayoutGroup));
        gridObject.transform.SetParent(panelRect, false);

        RectTransform gridRect = gridObject.GetComponent<RectTransform>();
        gridRect.anchorMin = new Vector2(0f, 1f);
        gridRect.anchorMax = new Vector2(0f, 1f);
        gridRect.pivot = new Vector2(0f, 1f);
        gridRect.anchoredPosition = new Vector2(18f, -66f);
        gridRect.sizeDelta = new Vector2(328f, 280f);

        GridLayoutGroup grid = gridObject.GetComponent<GridLayoutGroup>();
        grid.cellSize = slotSize;
        grid.spacing = slotSpacing;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = Mathf.Max(1, columns);
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.childAlignment = TextAnchor.UpperLeft;

        return gridRect;
    }

    private TextMeshProUGUI CreateFooter(RectTransform panelRect)
    {
        TextMeshProUGUI footer = CreateText(
            "FooterLabel",
            panelRect,
            "Click item để sử dụng",
            15f,
            FontStyles.Normal,
            TextAlignmentOptions.Left);
        RectTransform footerRect = footer.rectTransform;
        footerRect.anchorMin = new Vector2(0f, 0f);
        footerRect.anchorMax = new Vector2(1f, 0f);
        footerRect.pivot = new Vector2(0.5f, 0f);
        footerRect.anchoredPosition = new Vector2(0f, 16f);
        footerRect.offsetMin = new Vector2(18f, 16f);
        footerRect.offsetMax = new Vector2(-18f, 46f);
        footer.color = new Color(0.75f, 0.90f, 0.94f, 0.92f);
        footer.enableWordWrapping = true;

        return footer;
    }

    private void RebuildSlotGrid()
    {
        if (slotGridRoot == null)
        {
            return;
        }

        for (int i = slotGridRoot.childCount - 1; i >= 0; i--)
        {
            GameObject child = slotGridRoot.GetChild(i).gameObject;
            if (Application.isPlaying)
            {
                Destroy(child);
            }
            else
            {
                DestroyImmediate(child);
            }
        }

        slotViews.Clear();

        int slotCount = playerInventory != null ? playerInventory.SlotCount : 20;
        for (int i = 0; i < slotCount; i++)
        {
            int slotIndex = i;
            SlotView slotView = CreateSlotView(slotGridRoot, slotIndex);
            slotView.Button.onClick.AddListener(() => HandleSlotClicked(slotIndex));
            slotViews.Add(slotView);
        }
    }

    private SlotView CreateSlotView(Transform parent, int slotIndex)
    {
        GameObject slotObject = new GameObject(
            $"Slot_{slotIndex:00}",
            typeof(RectTransform),
            typeof(Image),
            typeof(Button));
        slotObject.transform.SetParent(parent, false);

        Image slotImage = slotObject.GetComponent<Image>();
        slotImage.color = slotEmptyColor;
        slotImage.raycastTarget = true;

        Outline outline = slotObject.AddComponent<Outline>();
        outline.effectColor = slotOutlineColor;
        outline.effectDistance = new Vector2(1f, -1f);

        Button button = slotObject.GetComponent<Button>();
        button.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.92f);
        colors.pressedColor = new Color(0.85f, 0.95f, 1f, 0.86f);
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(1f, 1f, 1f, 0.5f);
        button.colors = colors;

        Image icon = CreateImage("Icon", slotObject.transform, Color.white, stretchToParent: false);
        RectTransform iconRect = icon.rectTransform;
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = Vector2.zero;
        iconRect.sizeDelta = new Vector2(36f, 36f);
        icon.preserveAspect = true;
        icon.raycastTarget = false;

        TextMeshProUGUI placeholder = CreateText(
            "Placeholder",
            slotObject.transform,
            string.Empty,
            24f,
            FontStyles.Bold,
            TextAlignmentOptions.Center);
        StretchToParent(placeholder.rectTransform);
        placeholder.color = placeholderColor;
        placeholder.raycastTarget = false;

        TextMeshProUGUI amountText = CreateText(
            "Amount",
            slotObject.transform,
            string.Empty,
            18f,
            FontStyles.Bold,
            TextAlignmentOptions.BottomRight);
        StretchToParent(amountText.rectTransform);
        amountText.rectTransform.offsetMin = new Vector2(4f, 4f);
        amountText.rectTransform.offsetMax = new Vector2(-4f, -4f);
        amountText.color = amountColor;
        amountText.raycastTarget = false;

        return new SlotView
        {
            Button = button,
            Background = slotImage,
            Icon = icon,
            AmountLabel = amountText,
            PlaceholderLabel = placeholder
        };
    }

    private void RefreshSlots()
    {
        if (playerInventory == null)
        {
            return;
        }

        EnsureInventoryUI();
        if (slotViews.Count != playerInventory.SlotCount)
        {
            RebuildSlotGrid();
        }

        IReadOnlyList<PlayerInventory.InventorySlot> slots = playerInventory.Slots;
        for (int i = 0; i < slotViews.Count && i < slots.Count; i++)
        {
            UpdateSlot(slotViews[i], slots[i]);
        }

        if (statusLabel != null && !isInventoryOpen)
        {
            statusLabel.text = "Click item để sử dụng";
        }
    }

    private void UpdateSlot(SlotView slotView, PlayerInventory.InventorySlot slot)
    {
        if (slotView == null || slot == null)
        {
            return;
        }

        if (slot.IsEmpty || slot.Item == null)
        {
            slotView.Background.color = slotEmptyColor;
            slotView.Icon.enabled = false;
            slotView.Icon.sprite = null;
            slotView.PlaceholderLabel.text = string.Empty;
            slotView.AmountLabel.text = string.Empty;
            return;
        }

        slotView.Background.color = slotFilledColor;
        slotView.Icon.sprite = slot.Item.Icon;
        slotView.Icon.enabled = slot.Item.Icon != null;
        slotView.PlaceholderLabel.text = slot.Item.Icon == null
            ? GetFallbackLabel(slot.Item.DisplayName)
            : string.Empty;
        slotView.AmountLabel.text = slot.Amount > 1 ? slot.Amount.ToString() : string.Empty;
    }

    private void HandleSlotClicked(int slotIndex)
    {
        if (!isInventoryOpen || playerInventory == null)
        {
            return;
        }

        playerInventory.TryUseSlot(slotIndex);
    }

    private void HandleInventoryFeedback(string message)
    {
        if (statusLabel == null || string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        statusLabel.text = message;
    }

    private void CacheAndDisableControls()
    {
        cachedControlStates.Clear();

        CacheBehaviour(playerMovement);
        CacheBehaviour(jump);
        CacheBehaviour(playerLook);
        CacheBehaviour(zoom);
        CacheBehaviour(pickUpScript);
        CacheBehaviour(actionScript);
        CacheBehaviour(cuttingTreeSystem);

        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
        }
    }

    private void RestoreControls()
    {
        foreach (KeyValuePair<Behaviour, bool> state in cachedControlStates)
        {
            if (state.Key != null)
            {
                state.Key.enabled = state.Value;
            }
        }

        cachedControlStates.Clear();
    }

    private void CacheBehaviour(Behaviour behaviour)
    {
        if (behaviour == null || cachedControlStates.ContainsKey(behaviour))
        {
            return;
        }

        cachedControlStates.Add(behaviour, behaviour.enabled);
        behaviour.enabled = false;
    }

    private void SetInventoryVisible(bool isVisible)
    {
        if (inventoryRoot == null || inventoryCanvasGroup == null)
        {
            return;
        }

        inventoryRoot.SetActive(isVisible);
        inventoryCanvasGroup.alpha = isVisible ? 1f : 0f;
        inventoryCanvasGroup.interactable = isVisible;
        inventoryCanvasGroup.blocksRaycasts = isVisible;
    }

    private void SetBlurActive(bool shouldEnable)
    {
        if (!EnsureBlurEffect())
        {
            return;
        }

        if (shouldEnable)
        {
            if (!blurStateCaptured)
            {
                blurOriginalActive = blurEffect.active;
                blurOriginalMode = blurEffect.mode.value;
                blurOriginalStart = blurEffect.gaussianStart.value;
                blurOriginalEnd = blurEffect.gaussianEnd.value;
                blurOriginalRadius = blurEffect.gaussianMaxRadius.value;
                blurStateCaptured = true;
            }

            blurEffect.active = true;
            blurEffect.mode.overrideState = true;
            blurEffect.gaussianStart.overrideState = true;
            blurEffect.gaussianEnd.overrideState = true;
            blurEffect.gaussianMaxRadius.overrideState = true;
            blurEffect.mode.value = DepthOfFieldMode.Gaussian;
            blurEffect.gaussianStart.value = blurGaussianStart;
            blurEffect.gaussianEnd.value = blurGaussianEnd;
            blurEffect.gaussianMaxRadius.value = blurRadius;
            return;
        }

        if (!blurStateCaptured)
        {
            return;
        }

        blurEffect.active = blurOriginalActive;
        blurEffect.mode.value = blurOriginalMode;
        blurEffect.gaussianStart.value = blurOriginalStart;
        blurEffect.gaussianEnd.value = blurOriginalEnd;
        blurEffect.gaussianMaxRadius.value = blurOriginalRadius;
        blurStateCaptured = false;
    }

    private bool EnsureBlurEffect()
    {
        if (blurEffect != null)
        {
            return true;
        }

        blurVolume ??= FindBestBlurVolume();
        if (blurVolume == null)
        {
            return false;
        }

        VolumeProfile profile = blurVolume.profile;
        if (profile == null)
        {
            return false;
        }

        if (!profile.TryGet(out blurEffect))
        {
            blurEffect = profile.Add<DepthOfField>(true);
            blurEffect.active = false;
        }

        return blurEffect != null;
    }

    private Volume FindBestBlurVolume()
    {
        Volume[] volumes = FindObjectsByType<Volume>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < volumes.Length; i++)
        {
            if (volumes[i] != null && volumes[i].isGlobal)
            {
                return volumes[i];
            }
        }

        return FindFirstObjectByType<Volume>();
    }

    private static string GetFallbackLabel(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "?";
        }

        return value.Substring(0, 1).ToUpperInvariant();
    }

    private static Image CreateImage(string objectName, Transform parent, Color color, bool stretchToParent)
    {
        GameObject imageObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        imageObject.transform.SetParent(parent, false);

        Image image = imageObject.GetComponent<Image>();
        image.color = color;

        if (stretchToParent)
        {
            StretchToParent(image.rectTransform);
        }

        return image;
    }

    private static TextMeshProUGUI CreateText(
        string objectName,
        Transform parent,
        string textValue,
        float fontSize,
        FontStyles fontStyle,
        TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = textValue;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;

        return text;
    }

    private static TextMeshProUGUI FindText(Transform root, string relativePath)
    {
        if (root == null)
        {
            return null;
        }

        Transform child = root.Find(relativePath);
        return child != null ? child.GetComponent<TextMeshProUGUI>() : null;
    }

    private static void StretchToParent(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.anchoredPosition = Vector2.zero;
    }
}
