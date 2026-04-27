using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class CampingCookingModeController : MonoBehaviour
{
    [Header("Cooking Mode")]
    [SerializeField] private KeyCode exitCookingModeKey = KeyCode.Q;
    [SerializeField] private string cookingModePrompt = "Che do nau an - B mo tui do | Q de thoat";

    private readonly Dictionary<Behaviour, bool> cachedControlStates = new Dictionary<Behaviour, bool>();

    private PlayerUI playerUI;
    private InventoryUIController inventoryUIController;
    private PlayerMovement playerMovement;
    private Jump jump;
    private PlayerLook playerLook;
    private Zoom zoom;
    private PickUpScript pickUpScript;
    private ActionScript actionScript;
    private CuttingTreeSystem cuttingTreeSystem;
    private Rigidbody playerRigidbody;
    private Camera mainCamera;

    private Transform activeCamp;

    public bool IsCookingModeActive => activeCamp != null;

    private void Awake()
    {
        TryAutoAssignReferences();
    }

    private void OnEnable()
    {
        TryAutoAssignReferences();
    }

    private void OnDisable()
    {
        ExitCookingMode();
    }

    private void Update()
    {
        if (!IsCookingModeActive)
        {
            return;
        }

        if (activeCamp == null)
        {
            ExitCookingMode();
            return;
        }

        if (inventoryUIController != null && inventoryUIController.IsInventoryOpen)
        {
            return;
        }

        if (Input.GetKeyDown(exitCookingModeKey))
        {
            ExitCookingMode();
            return;
        }

        if (playerUI != null)
        {
            playerUI.UpdatePrompt(cookingModePrompt);
        }
    }

    public bool EnterCookingMode(Transform campTransform)
    {
        if (campTransform == null)
        {
            return false;
        }

        TryAutoAssignReferences();

        if (IsCookingModeActive)
        {
            if (activeCamp == campTransform)
            {
                return true;
            }

            ExitCookingMode();
        }

        if (inventoryUIController != null && inventoryUIController.IsInventoryOpen)
        {
            inventoryUIController.SetInventoryOpen(false);
        }

        activeCamp = campTransform;
        CacheAndDisableControls();

        if (playerUI != null)
        {
            playerUI.HideInteractionContent();
            playerUI.UpdatePrompt(cookingModePrompt);
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        return true;
    }

    public void ExitCookingMode()
    {
        if (inventoryUIController != null && inventoryUIController.IsInventoryOpen)
        {
            inventoryUIController.SetInventoryOpen(false);
        }

        RestoreControls();
        activeCamp = null;

        if (playerUI != null)
        {
            playerUI.UpdatePrompt(string.Empty);
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void TryAutoAssignReferences()
    {
        playerUI ??= GetComponent<PlayerUI>();
        playerUI ??= FindFirstObjectByType<PlayerUI>();
        inventoryUIController ??= GetComponent<InventoryUIController>();
        inventoryUIController ??= FindFirstObjectByType<InventoryUIController>();

        playerMovement ??= GetComponent<PlayerMovement>();
        jump ??= GetComponent<Jump>();
        actionScript ??= GetComponent<ActionScript>();
        playerRigidbody ??= GetComponent<Rigidbody>();
        mainCamera ??= Camera.main;

        if (mainCamera != null)
        {
            playerLook ??= mainCamera.GetComponent<PlayerLook>();
            zoom ??= mainCamera.GetComponent<Zoom>();
            pickUpScript ??= mainCamera.GetComponent<PickUpScript>();
            cuttingTreeSystem ??= mainCamera.GetComponent<CuttingTreeSystem>();
        }
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
}
