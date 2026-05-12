using UnityEngine;

public class MenuManager : MonoBehaviour
{
    public static MenuManager Instance { get; set; }

    public GameObject menuCanvas;
    public GameObject uiCanvas;

    public GameObject saveMenu;
    public GameObject loadMenu;
    public GameObject settingsMenu;
    public GameObject subMenu;

    public bool isMenuOpen;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Z) && !isMenuOpen)
        {
            uiCanvas.SetActive(false);
            menuCanvas.SetActive(true);

            isMenuOpen = true;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            FindFirstObjectByType<PickUpScript>().enabled = false;
            FindFirstObjectByType<PlayerLook>().enabled = false;
            FindFirstObjectByType<MouseMovement>().enabled = false;
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

            if (FindFirstObjectByType<InventoryUIController>().IsInventoryOpen == false)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = false; 
            }

            FindFirstObjectByType<PickUpScript>().enabled = true;
            FindFirstObjectByType<PlayerLook>().enabled = true;
            FindFirstObjectByType<MouseMovement>().enabled = true;
        }
    }

    public void BackSubMenu()
    {
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

        if (FindFirstObjectByType<InventoryUIController>().IsInventoryOpen == false)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = false;
        }

        FindFirstObjectByType<PickUpScript>().enabled = true;
        FindFirstObjectByType<PlayerLook>().enabled = true;
        FindFirstObjectByType<MouseMovement>().enabled = true;
    }
}
