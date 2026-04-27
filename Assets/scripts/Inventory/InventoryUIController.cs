using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public class InventoryUIController : MonoBehaviour
{
    private const string DefaultFooterMessage = "Chuot trai de dung | Chuot phai de xem info";
    private const string DefaultCampingPrefabAssetPath = "Assets/model/campfire/source/camping.prefab";
    private const string DefaultFishPrefabAssetPath = "Assets/model/Fish/Prefabs/fish01.prefab";
    private const string DefaultMeatPrefabAssetPath = "Assets/model/Meat/source/meat.prefab";

    private sealed class SlotView
    {
        public Button Button;
        public Image Background;
        public Image Icon;
        public TextMeshProUGUI AmountLabel;
    }

    private enum PlacementState
    {
        Inactive,
        CampingPreview,
        IngredientPreview
    }

    [Header("Input")]
    [SerializeField] private KeyCode toggleInventoryKey = KeyCode.B;

    [Header("References")]
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private PlayerUI playerUI;
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private Volume blurVolume;

    [Header("Layout")]
    [SerializeField] [Min(1)] private int columns = 5;
    [SerializeField] private Vector2 slotSize = new Vector2(60f, 60f);
    [SerializeField] private Vector2 slotSpacing = new Vector2(8f, 8f);

    [Header("Look")]
    [SerializeField] private Color backdropColor = new Color(0.02f, 0.17f, 0.20f, 0.58f);
    [SerializeField] private Color panelColor = new Color(0.03f, 0.14f, 0.18f, 0.80f);
    [SerializeField] private Color slotEmptyColor = new Color(0.06f, 0.20f, 0.24f, 0.78f);
    [SerializeField] private Color slotFilledColor = new Color(0.13f, 0.28f, 0.32f, 0.96f);
    [SerializeField] private Color slotOutlineColor = new Color(0.21f, 0.68f, 0.77f, 0.56f);
    [SerializeField] private Color amountColor = new Color(1f, 0.42f, 0.34f, 1f);

    [Header("Blur")]
    [SerializeField] private float blurGaussianStart = 0.1f;
    [SerializeField] private float blurGaussianEnd = 4f;
    [SerializeField] private float blurRadius = 1f;

    [Header("Camping Placement")]
    [SerializeField] private string campingItemId = "wood_log";
    [SerializeField] private GameObject campingPrefab;
    [SerializeField] private LayerMask campingPlacementLayers = Physics.DefaultRaycastLayers;
    [SerializeField] private float campingPlacementDistance = 12f;
    [SerializeField] [Range(0.1f, 1f)] private float campingSurfaceMinUpDot = 0.55f;

    [Header("Cooking Ingredient Placement")]
    [SerializeField] private string fishItemId = "river_fish";
    [SerializeField] private GameObject fishPlacementPrefab;
    [SerializeField] private string meatItemId = "boar_meat";
    [SerializeField] private GameObject meatPlacementPrefab;
    [SerializeField] private float ingredientPlacementDistance = 12f;

    private readonly List<SlotView> slotViews = new List<SlotView>();
    private readonly Dictionary<Behaviour, bool> cachedControlStates = new Dictionary<Behaviour, bool>();
    private GameObject inventoryRoot;
    private CanvasGroup inventoryCanvasGroup;
    private TextMeshProUGUI statusLabel;
    private RectTransform slotGridRoot;

    private Camera mainCamera;
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
    private PlacementState placementState;
    private GameObject activeCampingInstance;
    private InventoryItemDefinition activeCampingItemDefinition;
    private int activeCampingSlotIndex = -1;
    private float activeCampingBottomOffset;
    private bool canPlaceCamping;
    private GameObject activeIngredientInstance;
    private InventoryItemDefinition activeIngredientItemDefinition;
    private int activeIngredientSlotIndex = -1;
    private float activeIngredientBottomOffset;
    private Quaternion activeIngredientRotation = Quaternion.identity;
    private bool canSubmitIngredientToCamping;

    public bool IsInventoryOpen => isInventoryOpen;
    public bool IsPlacementPreviewActive => placementState != PlacementState.Inactive;

    private void Reset()
    {
        TryAutoAssignReferences();
        TryAssignDefaultCampingPrefab();
        TryAssignDefaultIngredientPrefabs();
        EnsureInventoryUI();
        RefreshSlots();
        SetInventoryVisible(false);
    }

    private void Awake()
    {
        TryAutoAssignReferences();
        TryAssignDefaultCampingPrefab();
        TryAssignDefaultIngredientPrefabs();
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

        CleanupActivePlacement(true);
        RestoreControls();
        SetBlurActive(false);
    }

    private void OnValidate()
    {
        TryAutoAssignReferences();
        TryAssignDefaultCampingPrefab();
        TryAssignDefaultIngredientPrefabs();
        columns = Mathf.Max(1, columns);
        slotSize.x = Mathf.Max(48f, slotSize.x);
        slotSize.y = Mathf.Max(48f, slotSize.y);
        slotSpacing.x = Mathf.Max(0f, slotSpacing.x);
        slotSpacing.y = Mathf.Max(0f, slotSpacing.y);
        blurRadius = Mathf.Clamp(blurRadius, 0.1f, 1f);
        blurGaussianEnd = Mathf.Max(blurGaussianStart + 0.1f, blurGaussianEnd);
        campingPlacementDistance = Mathf.Max(1f, campingPlacementDistance);
        ingredientPlacementDistance = Mathf.Max(1f, ingredientPlacementDistance);
    }

    private void Update()
    {
        if (placementState != PlacementState.Inactive)
        {
            UpdatePlacementState();
            return;
        }

        if (DialogueController.IsDialogueActive)
        {
            if (isInventoryOpen)
            {
                SetInventoryOpen(false);
            }

            return;
        }

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

        if (placementState != PlacementState.Inactive)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
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
        if (shouldOpen && DialogueController.IsDialogueActive)
        {
            return;
        }

        if (shouldOpen && placementState != PlacementState.Inactive)
        {
            return;
        }

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
            SetStatusMessage(DefaultFooterMessage);

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
        mainCamera ??= Camera.main;

        playerMovement ??= GetComponent<PlayerMovement>();
        jump ??= GetComponent<Jump>();
        actionScript ??= GetComponent<ActionScript>();
        playerRigidbody ??= GetComponent<Rigidbody>();

        if (playerLook == null || zoom == null || pickUpScript == null || cuttingTreeSystem == null)
        {
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
            true);
        backdrop.raycastTarget = true;

        RectTransform panelRect = CreatePanel();
        CreateHeader(panelRect);
        slotGridRoot = CreateSlotGrid(panelRect);
        statusLabel = CreateFooter(panelRect);

        RebuildSlotGrid();
    }

    private RectTransform CreatePanel()
    {
        Image panelImage = CreateImage("InventoryPanel", inventoryRoot.transform, panelColor, false);
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
            "Tui do",
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
            "B de dong",
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
            DefaultFooterMessage,
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
        footer.textWrappingMode = TextWrappingModes.Normal;

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
            SlotView slotView = CreateSlotView(slotGridRoot, i);
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

        InventorySlotClickHandler clickHandler = slotObject.AddComponent<InventorySlotClickHandler>();
        clickHandler.Initialize(slotIndex, HandleSlotClicked);

        Image icon = CreateImage("Icon", slotObject.transform, Color.white, false);
        RectTransform iconRect = icon.rectTransform;
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = Vector2.zero;
        iconRect.sizeDelta = new Vector2(36f, 36f);
        icon.preserveAspect = true;
        icon.raycastTarget = false;

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
            AmountLabel = amountText
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
            SetStatusMessage(DefaultFooterMessage);
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
            slotView.AmountLabel.text = string.Empty;
            return;
        }

        slotView.Background.color = slotFilledColor;
        slotView.Icon.sprite = slot.Item.Icon;
        slotView.Icon.enabled = slot.Item.Icon != null;
        slotView.AmountLabel.text = slot.Amount > 1 ? slot.Amount.ToString() : string.Empty;
    }

    private void HandleSlotClicked(int slotIndex, PointerEventData.InputButton button)
    {
        if (!isInventoryOpen || playerInventory == null)
        {
            return;
        }

        if (button == PointerEventData.InputButton.Right)
        {
            HandleSlotRightClick(slotIndex);
            return;
        }

        if (button != PointerEventData.InputButton.Left)
        {
            return;
        }

        HandleSlotLeftClick(slotIndex);
    }

    private void HandleSlotLeftClick(int slotIndex)
    {
        PlayerInventory.InventorySlot slot = GetInventorySlot(slotIndex);
        if (slot == null || slot.IsEmpty || slot.Item == null)
        {
            SetStatusMessage(DefaultFooterMessage);
            return;
        }

        if (IsCampingItem(slot.Item))
        {
            TryBeginCampingPlacement(slotIndex, slot.Item);
            return;
        }

        if (IsCookingIngredientItem(slot.Item))
        {
            TryBeginIngredientPlacement(slotIndex, slot.Item);
            return;
        }

        playerInventory.TryUseSlot(slotIndex);
    }

    private void HandleSlotRightClick(int slotIndex)
    {
        PlayerInventory.InventorySlot slot = GetInventorySlot(slotIndex);
        if (slot == null || slot.IsEmpty || slot.Item == null)
        {
            SetStatusMessage(DefaultFooterMessage);
            return;
        }

        string description = string.IsNullOrWhiteSpace(slot.Item.Description)
            ? "Khong co mo ta."
            : slot.Item.Description;
        string actionHint = IsCampingItem(slot.Item)
            ? "Chuot trai de dat camping."
            : IsCookingIngredientItem(slot.Item)
                ? "Vao che do nau an roi chuot trai de dua ra camping."
                : slot.Item.CanUse
                ? "Chuot trai de dung."
                : slot.Item.CannotUseMessage;

        SetStatusMessage($"{slot.Item.DisplayName} x{slot.Amount}\n{description}\n{actionHint}");
    }

    private void HandleInventoryFeedback(string message)
    {
        SetStatusMessage(message);
    }

    private void CacheAndDisableControls()
    {
        cachedControlStates.Clear();
        CacheGameplayBehaviours(cachedControlStates);

        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
        }
    }

    private void RestoreControls()
    {
        RestoreCachedControls(cachedControlStates);
    }

    private void CacheGameplayBehaviours(Dictionary<Behaviour, bool> cache)
    {
        CacheBehaviour(cache, playerMovement);
        CacheBehaviour(cache, jump);
        CacheBehaviour(cache, playerLook);
        CacheBehaviour(cache, zoom);
        CacheBehaviour(cache, pickUpScript);
        CacheBehaviour(cache, actionScript);
        CacheBehaviour(cache, cuttingTreeSystem);
    }

    private void CacheBehaviour(Dictionary<Behaviour, bool> cache, Behaviour behaviour)
    {
        if (behaviour == null || cache.ContainsKey(behaviour))
        {
            return;
        }

        cache.Add(behaviour, behaviour.enabled);
        behaviour.enabled = false;
    }

    private static void RestoreCachedControls(Dictionary<Behaviour, bool> cache)
    {
        foreach (KeyValuePair<Behaviour, bool> state in cache)
        {
            if (state.Key != null)
            {
                state.Key.enabled = state.Value;
            }
        }

        cache.Clear();
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

    private PlayerInventory.InventorySlot GetInventorySlot(int slotIndex)
    {
        if (playerInventory == null || slotIndex < 0 || slotIndex >= playerInventory.Slots.Count)
        {
            return null;
        }

        return playerInventory.Slots[slotIndex];
    }

    private void SetStatusMessage(string message)
    {
        if (statusLabel == null)
        {
            return;
        }

        statusLabel.text = string.IsNullOrWhiteSpace(message)
            ? DefaultFooterMessage
            : message;
    }

    private bool IsCampingItem(InventoryItemDefinition itemDefinition)
    {
        return itemDefinition != null &&
               !string.IsNullOrWhiteSpace(campingItemId) &&
               string.Equals(itemDefinition.ItemId, campingItemId, StringComparison.OrdinalIgnoreCase);
    }

    private bool IsCookingIngredientItem(InventoryItemDefinition itemDefinition)
    {
        return itemDefinition != null &&
               (MatchesItemId(itemDefinition, fishItemId) || MatchesItemId(itemDefinition, meatItemId));
    }

    private bool MatchesItemId(InventoryItemDefinition itemDefinition, string itemId)
    {
        return itemDefinition != null &&
               !string.IsNullOrWhiteSpace(itemId) &&
               string.Equals(itemDefinition.ItemId, itemId, StringComparison.OrdinalIgnoreCase);
    }

    private void TryBeginCampingPlacement(int slotIndex, InventoryItemDefinition itemDefinition)
    {
        if (itemDefinition == null)
        {
            return;
        }

        if (!EnsureCampingPlacementReady())
        {
            SetStatusMessage("Camping prefab hoac camera chua san sang.");
            return;
        }

        SetInventoryOpen(false);
        BeginCampingPlacement(slotIndex, itemDefinition);
    }

    private void TryBeginIngredientPlacement(int slotIndex, InventoryItemDefinition itemDefinition)
    {
        if (itemDefinition == null)
        {
            return;
        }

        CampingCookingModeController cookingModeController = FindFirstObjectByType<CampingCookingModeController>();
        if (cookingModeController == null ||
            !cookingModeController.IsCookingModeActive ||
            cookingModeController.ActiveCamp == null)
        {
            SetStatusMessage("Can vao che do nau an truoc khi lay do nau an ra.");
            return;
        }

        if (!TryGetIngredientPlacementPrefab(itemDefinition, out GameObject ingredientPrefab))
        {
            SetStatusMessage("Prefab do nau an chua san sang.");
            return;
        }

        if (mainCamera == null)
        {
            TryAutoAssignReferences();
        }

        if (mainCamera == null)
        {
            SetStatusMessage("Camera chua san sang.");
            return;
        }

        SetInventoryOpen(false);
        BeginIngredientPlacement(slotIndex, itemDefinition, ingredientPrefab);
    }

    private bool EnsureCampingPlacementReady()
    {
        TryAutoAssignReferences();
        TryAssignDefaultCampingPrefab();
        return mainCamera != null && campingPrefab != null;
    }

    private bool TryGetIngredientPlacementPrefab(
        InventoryItemDefinition itemDefinition,
        out GameObject ingredientPrefab)
    {
        ingredientPrefab = null;
        if (itemDefinition == null)
        {
            return false;
        }

        TryAssignDefaultIngredientPrefabs();

        if (MatchesItemId(itemDefinition, fishItemId))
        {
            ingredientPrefab = fishPlacementPrefab;
            return ingredientPrefab != null;
        }

        if (MatchesItemId(itemDefinition, meatItemId))
        {
            ingredientPrefab = meatPlacementPrefab;
            return ingredientPrefab != null;
        }

        return false;
    }

    private void BeginCampingPlacement(int slotIndex, InventoryItemDefinition itemDefinition)
    {
        CleanupActivePlacement(false);

        activeCampingSlotIndex = slotIndex;
        activeCampingItemDefinition = itemDefinition;

        activeCampingInstance = Instantiate(campingPrefab);
        activeCampingInstance.name = campingPrefab.name;
        activeCampingBottomOffset = CalculateCampingBottomOffset(activeCampingInstance);
        SetCampingCollidersEnabled(activeCampingInstance, false);
        placementState = PlacementState.CampingPreview;
        canPlaceCamping = false;

        if (playerUI != null)
        {
            playerUI.HideInteractionContent();
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        UpdateCampingPreview();
    }

    private void BeginIngredientPlacement(
        int slotIndex,
        InventoryItemDefinition itemDefinition,
        GameObject ingredientPrefab)
    {
        CleanupActivePlacement(false);

        activeIngredientSlotIndex = slotIndex;
        activeIngredientItemDefinition = itemDefinition;
        activeIngredientInstance = Instantiate(ingredientPrefab);
        activeIngredientInstance.name = ingredientPrefab.name;
        activeIngredientBottomOffset = CalculateCampingBottomOffset(activeIngredientInstance);
        activeIngredientRotation = activeIngredientInstance.transform.rotation;
        SetCampingCollidersEnabled(activeIngredientInstance, false);
        canSubmitIngredientToCamping = false;
        placementState = PlacementState.IngredientPreview;

        if (playerUI != null)
        {
            playerUI.HideInteractionContent();
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        UpdateIngredientPreview();
    }

    private void UpdatePlacementState()
    {
        switch (placementState)
        {
            case PlacementState.CampingPreview:
                UpdateCampingPlacementState();
                break;

            case PlacementState.IngredientPreview:
                UpdateIngredientPlacementState();
                break;
        }
    }

    private void UpdateCampingPlacementState()
    {
        if (placementState == PlacementState.CampingPreview)
        {
            UpdateCampingPreview();

            if (canPlaceCamping && Input.GetMouseButtonDown(0))
            {
                PlaceCamping();
            }
        }
    }

    private void UpdateIngredientPlacementState()
    {
        if (placementState != PlacementState.IngredientPreview)
        {
            return;
        }

        if (Input.GetKeyDown(toggleInventoryKey))
        {
            CleanupIngredientPlacement(false);
            SetInventoryOpen(true);
            return;
        }

        UpdateIngredientPreview();

        if (canSubmitIngredientToCamping && Input.GetMouseButtonDown(0))
        {
            SubmitIngredientToCamping();
        }
    }

    private void UpdateCampingPreview()
    {
        if (activeCampingInstance == null)
        {
            CleanupCampingPlacement(true);
            return;
        }

        if (TryGetCampingPlacementPose(out Vector3 position, out Quaternion rotation, out bool isValid))
        {
            activeCampingInstance.transform.SetPositionAndRotation(position, rotation);
            canPlaceCamping = isValid;
        }
        else
        {
            canPlaceCamping = false;
        }

        if (playerUI != null)
        {
            playerUI.UpdatePrompt(canPlaceCamping
                ? "Chuot trai de dat camping"
                : "Can dua camping len mat dat hop le");
        }
    }

    private void UpdateIngredientPreview()
    {
        if (activeIngredientInstance == null)
        {
            CleanupIngredientPlacement(true);
            return;
        }

        if (TryGetIngredientPlacementPose(out Vector3 position, out Quaternion rotation, out bool isValid))
        {
            activeIngredientInstance.transform.SetPositionAndRotation(position, rotation);
            canSubmitIngredientToCamping = isValid;
        }
        else
        {
            canSubmitIngredientToCamping = false;
        }

        if (playerUI != null)
        {
            string itemName = activeIngredientItemDefinition != null
                ? activeIngredientItemDefinition.DisplayName
                : "Do nau an";

            playerUI.UpdatePrompt(canSubmitIngredientToCamping
                ? $"Chuot trai de bo {itemName} vao camping"
                : $"Dua {itemName} cham vao camping | B mo tui do");
        }
    }

    private bool TryGetCampingPlacementPose(out Vector3 position, out Quaternion rotation, out bool isValid)
    {
        position = Vector3.zero;
        rotation = Quaternion.identity;
        isValid = false;

        if (mainCamera == null)
        {
            return false;
        }

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(
                ray,
                out RaycastHit hit,
                campingPlacementDistance,
                campingPlacementLayers,
                QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        Vector3 forward = playerMovement != null ? playerMovement.transform.forward : mainCamera.transform.forward;
        forward = Vector3.ProjectOnPlane(forward, Vector3.up);
        if (forward.sqrMagnitude <= 0.0001f)
        {
            forward = Vector3.forward;
        }

        position = hit.point + Vector3.up * activeCampingBottomOffset;
        rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
        isValid = Vector3.Dot(hit.normal, Vector3.up) >= campingSurfaceMinUpDot;
        return true;
    }

    private bool TryGetIngredientPlacementPose(out Vector3 position, out Quaternion rotation, out bool isValid)
    {
        position = Vector3.zero;
        rotation = activeIngredientRotation;
        isValid = false;

        if (mainCamera == null)
        {
            return false;
        }

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(
                ray,
                out RaycastHit hit,
                ingredientPlacementDistance,
                campingPlacementLayers,
                QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        position = hit.point + Vector3.up * activeIngredientBottomOffset;
        rotation = activeIngredientRotation;
        activeIngredientInstance.transform.SetPositionAndRotation(position, rotation);
        isValid = FindIntersectingCamping(activeIngredientInstance) != null;
        return true;
    }

    private void PlaceCamping()
    {
        if (placementState != PlacementState.CampingPreview ||
            activeCampingInstance == null ||
            activeCampingItemDefinition == null)
        {
            return;
        }

        if (!canPlaceCamping)
        {
            return;
        }

        if (playerInventory == null ||
            !playerInventory.TryConsumeSlot(activeCampingSlotIndex, 1, activeCampingItemDefinition))
        {
            CleanupCampingPlacement(true);
            SetStatusMessage("Khuc go khong con trong tui do.");
            return;
        }

        activeCampingSlotIndex = -1;
        SetCampingCollidersEnabled(activeCampingInstance, true);
        EnsureCampingInteraction(activeCampingInstance);
        activeCampingInstance = null;
        activeCampingItemDefinition = null;
        activeCampingBottomOffset = 0f;
        canPlaceCamping = false;
        placementState = PlacementState.Inactive;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (playerUI != null)
        {
            playerUI.UpdatePrompt(string.Empty);
        }
    }

    private void SubmitIngredientToCamping()
    {
        if (placementState != PlacementState.IngredientPreview ||
            activeIngredientInstance == null ||
            activeIngredientItemDefinition == null)
        {
            return;
        }

        if (!canSubmitIngredientToCamping)
        {
            return;
        }

        if (playerInventory == null ||
            !playerInventory.TryConsumeSlot(activeIngredientSlotIndex, 1, activeIngredientItemDefinition))
        {
            CleanupIngredientPlacement(true);
            SetStatusMessage("Vat pham nau an khong con trong tui do.");
            return;
        }

        CleanupIngredientPlacement(true);
    }

    private void CleanupCampingPlacement(bool restoreCursorState)
    {
        if (activeCampingInstance != null)
        {
            Destroy(activeCampingInstance);
        }

        activeCampingInstance = null;
        activeCampingItemDefinition = null;
        activeCampingSlotIndex = -1;
        activeCampingBottomOffset = 0f;
        canPlaceCamping = false;
        placementState = PlacementState.Inactive;

        if (restoreCursorState)
        {
            Cursor.lockState = isInventoryOpen ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = isInventoryOpen;
        }

        if (playerUI != null)
        {
            playerUI.UpdatePrompt(string.Empty);
        }
    }

    private void CleanupIngredientPlacement(bool restoreCursorState)
    {
        if (activeIngredientInstance != null)
        {
            Destroy(activeIngredientInstance);
        }

        activeIngredientInstance = null;
        activeIngredientItemDefinition = null;
        activeIngredientSlotIndex = -1;
        activeIngredientBottomOffset = 0f;
        activeIngredientRotation = Quaternion.identity;
        canSubmitIngredientToCamping = false;
        placementState = PlacementState.Inactive;

        if (restoreCursorState)
        {
            Cursor.lockState = isInventoryOpen ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = isInventoryOpen;
        }

        if (playerUI != null)
        {
            playerUI.UpdatePrompt(string.Empty);
        }
    }

    private void CleanupActivePlacement(bool restoreCursorState)
    {
        if (placementState == PlacementState.CampingPreview)
        {
            CleanupCampingPlacement(restoreCursorState);
            return;
        }

        if (placementState == PlacementState.IngredientPreview)
        {
            CleanupIngredientPlacement(restoreCursorState);
        }
    }

    private void TryAssignDefaultCampingPrefab()
    {
#if UNITY_EDITOR
        if (campingPrefab == null)
        {
            campingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DefaultCampingPrefabAssetPath);
        }
#endif
    }

    private void TryAssignDefaultIngredientPrefabs()
    {
#if UNITY_EDITOR
        if (fishPlacementPrefab == null)
        {
            fishPlacementPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DefaultFishPrefabAssetPath);
        }

        if (meatPlacementPrefab == null)
        {
            meatPlacementPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DefaultMeatPrefabAssetPath);
        }
#endif
    }

    private static void SetCampingCollidersEnabled(GameObject campingObject, bool isEnabled)
    {
        if (campingObject == null)
        {
            return;
        }

        Collider[] colliders = campingObject.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
            {
                colliders[i].enabled = isEnabled;
            }
        }
    }

    private static void EnsureCampingInteraction(GameObject campingObject)
    {
        if (campingObject == null)
        {
            return;
        }

        if (campingObject.GetComponent<CampingCookingInteractable>() == null)
        {
            campingObject.AddComponent<CampingCookingInteractable>();
        }
    }

    private static float CalculateCampingBottomOffset(GameObject campingObject)
    {
        if (!TryGetCampingBounds(campingObject, out Bounds bounds))
        {
            return 0f;
        }

        return campingObject.transform.position.y - bounds.min.y;
    }

    private CampingCookingInteractable FindIntersectingCamping(GameObject ingredientObject)
    {
        if (!TryGetCampingBounds(ingredientObject, out Bounds ingredientBounds))
        {
            return null;
        }

        CampingCookingModeController cookingModeController = FindFirstObjectByType<CampingCookingModeController>();
        if (cookingModeController == null ||
            !cookingModeController.IsCookingModeActive ||
            cookingModeController.ActiveCamp == null)
        {
            return null;
        }

        CampingCookingInteractable campingTarget =
            cookingModeController.ActiveCamp.GetComponent<CampingCookingInteractable>();

        if (campingTarget == null ||
            !campingTarget.isActiveAndEnabled ||
            !TryGetCampingBounds(campingTarget.gameObject, out Bounds campingBounds))
        {
            return null;
        }

        return ingredientBounds.Intersects(campingBounds) ? campingTarget : null;
    }

    private static bool TryGetCampingBounds(GameObject campingObject, out Bounds bounds)
    {
        bounds = default;
        if (campingObject == null)
        {
            return false;
        }

        Renderer[] renderers = campingObject.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length > 0)
        {
            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }
            }

            return true;
        }

        Collider[] colliders = campingObject.GetComponentsInChildren<Collider>(true);
        if (colliders.Length == 0)
        {
            return false;
        }

        bounds = colliders[0].bounds;
        for (int i = 1; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
            {
                bounds.Encapsulate(colliders[i].bounds);
            }
        }

        return true;
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
