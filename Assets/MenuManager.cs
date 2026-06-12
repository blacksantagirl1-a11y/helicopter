using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    // Singleton cho pause menu trong scene gameplay.
    public static MenuManager Instance { get; set; }

    // Canvas menu tam dung va HUD gameplay.
    public GameObject menuCanvas;
    public GameObject uiCanvas;

    // Cac man con trong pause menu.
    public GameObject saveMenu;
    public GameObject loadMenu;
    public GameObject settingsMenu;
    public GameObject subMenu;

    // Thong so hover cho cac nut trong sub menu.
    [SerializeField, Min(1f)] private float subMenuHighlightScale = 1.06f;
    [SerializeField, Min(0f)] private float subMenuButtonTweenDuration = 0.16f;
    [SerializeField] private Color subMenuButtonNormalColor = new Color(0.92f, 0.96f, 1f, 0.78f);
    [SerializeField] private Color subMenuButtonHoverColor = Color.white;

    // Luu cac nut submenu va trang thai control truoc khi pause de restore dung.
    private readonly Dictionary<Button, SubMenuButtonVisual> subMenuButtons = new Dictionary<Button, SubMenuButtonVisual>();
    private readonly Dictionary<Behaviour, bool> cachedControlStates = new Dictionary<Behaviour, bool>();
    private Button hoveredSubMenuButton;

    public bool isMenuOpen;

    private void Awake()
    {
        // Dam bao chi co mot MenuManager dieu khien pause menu.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }

        RegisterSubMenuButtons();
        RefreshSubMenuButtons(true);
    }

    private void Update()
    {
        // Phim Z dung de mo/dong pause menu trong gameplay.
        if (Input.GetKeyDown(KeyCode.Z) && !isMenuOpen)
        {
            uiCanvas.SetActive(false);
            menuCanvas.SetActive(true);

            isMenuOpen = true;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            CacheAndDisableMenuControls();
            Input.ResetInputAxes();
        }
        else if (Input.GetKeyDown(KeyCode.Z) && isMenuOpen)
        {
            saveMenu.SetActive(false);
            loadMenu.SetActive(false);
            settingsMenu.SetActive(false);
            subMenu.SetActive(true);

            menuCanvas.SetActive(false);
            uiCanvas.SetActive(true);

            isMenuOpen = false;

            InventoryUIController inventoryUIController = FindFirstObjectByType<InventoryUIController>();
            if (inventoryUIController == null || inventoryUIController.IsInventoryOpen == false)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false; 
            }

            Input.ResetInputAxes();
            RestoreMenuControls();
        }
    }

    public void BackSubMenu()
    {
        // Nut back dua nguoi choi ve gameplay va khoi phuc control.
        if (!isMenuOpen)
        {
            return;
        }

        saveMenu.SetActive(false);
        loadMenu.SetActive(false);
        settingsMenu.SetActive(false);
        subMenu.SetActive(true);

        menuCanvas.SetActive(false);
        uiCanvas.SetActive(true);

        isMenuOpen = false;

        InventoryUIController inventoryUIController = FindFirstObjectByType<InventoryUIController>();
        if (inventoryUIController == null || inventoryUIController.IsInventoryOpen == false)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        Input.ResetInputAxes();
        RestoreMenuControls();
    }

    private void CacheAndDisableMenuControls()
    {
        // Luu trang thai cac script dieu khien roi tat chung de player khong di chuyen khi mo menu.
        cachedControlStates.Clear();

        CacheBehaviour(FindFirstObjectByType<PlayerMovement>());
        CacheBehaviour(FindFirstObjectByType<Jump>());
        CacheBehaviour(FindFirstObjectByType<Crouch>());
        CacheBehaviour(FindFirstObjectByType<ActionScript>());
        CacheBehaviour(FindFirstObjectByType<Zoom>());
        CacheBehaviour(FindFirstObjectByType<PickUpScript>());
        CacheBehaviour(FindFirstObjectByType<CuttingTreeSystem>());

        PlayerLook[] playerLooks = FindObjectsByType<PlayerLook>(FindObjectsSortMode.None);
        for (int i = 0; i < playerLooks.Length; i++)
        {
            CacheBehaviour(playerLooks[i]);
        }

        MouseMovement[] mouseMovements = FindObjectsByType<MouseMovement>(FindObjectsSortMode.None);
        for (int i = 0; i < mouseMovements.Length; i++)
        {
            CacheBehaviour(mouseMovements[i]);
        }

        Rigidbody playerRigidbody = FindFirstObjectByType<Rigidbody>();
        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
        }
    }

    private void RestoreMenuControls()
    {
        // Tra cac script dieu khien ve dung trang thai truoc khi menu duoc mo.
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
        // Them mot behaviour vao cache neu no ton tai va chua duoc luu.
        if (behaviour == null || cachedControlStates.ContainsKey(behaviour))
        {
            return;
        }

        cachedControlStates.Add(behaviour, behaviour.enabled);
        behaviour.enabled = false;
    }

    private void OnDisable()
    {
        RestoreMenuControls();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void RegisterSubMenuButtons()
    {
        // Tu dong gan relay hover cho tat ca nut trong submenu.
        subMenuButtons.Clear();

        if (subMenu == null)
        {
            return;
        }

        Button[] buttons = subMenu.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null)
            {
                continue;
            }

            subMenuButtons[button] = new SubMenuButtonVisual
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

            relay.Initialize(button, OnSubMenuButtonHoverChanged);
        }
    }

    private void OnSubMenuButtonHoverChanged(Button button, bool isHovered)
    {
        if (isHovered)
        {
            hoveredSubMenuButton = button;
        }
        else if (hoveredSubMenuButton == button)
        {
            hoveredSubMenuButton = null;
        }

        RefreshSubMenuButtons();
    }

    private void RefreshSubMenuButtons(bool immediate = false)
    {
        // Cap nhat visual cho tat ca nut dua tren nut dang hover.
        foreach (SubMenuButtonVisual visual in subMenuButtons.Values)
        {
            bool isHovered = visual.button == hoveredSubMenuButton;
            float targetScale = isHovered ? subMenuHighlightScale : 1f;
            Color targetColor = isHovered ? subMenuButtonHoverColor : subMenuButtonNormalColor;

            ApplySubMenuButtonVisual(visual, targetScale, targetColor, immediate);
        }
    }

    private void ApplySubMenuButtonVisual(SubMenuButtonVisual visual, float targetScale, Color targetColor, bool immediate)
    {
        // Dung tween cu truoc khi tao tween moi de hover khong bi cong don.
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
                    .DOScale(targetScale, subMenuButtonTweenDuration)
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
                    .DOColor(targetColor, subMenuButtonTweenDuration)
                    .SetEase(Ease.OutQuad)
                    .SetUpdate(true);
            }
        }
    }

    private sealed class SubMenuButtonVisual
    {
        public Button button;
        public TMP_Text label;
        public RectTransform rect;
    }

}
