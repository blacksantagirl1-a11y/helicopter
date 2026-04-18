using UnityEngine;

// Giu ten class cu de scene hien tai khong bi mat reference component.
public class PickUpScript : MonoBehaviour
{
    [Header("Raycast Interaction")]
    [Tooltip("Khoảng cách raycast để tìm object có thể tương tác")]
    [SerializeField] private float interactionRange = 5f;
    [Tooltip("Tham chiếu tới UI hiển thị prompt/hội thoại")]
    [SerializeField] private PlayerUI playerUI;
    [Tooltip("Prompt mặc định khi object không có PromptText riêng")]
    [SerializeField] private string defaultInteractionMessage = "Tuong tac";

    private FishingRob fishingRob;

    private void Start()
    {
        if (playerUI == null)
        {
            playerUI = FindFirstObjectByType<PlayerUI>();
        }

        fishingRob = FindFirstObjectByType<FishingRob>();
    }

    private void Update()
    {
        if (fishingRob == null)
        {
            fishingRob = FindFirstObjectByType<FishingRob>();
        }

        if (fishingRob != null && fishingRob.ShouldOverrideDefaultInteraction)
        {
            ShowPromptUI(fishingRob.CurrentPrompt);
        }
        else
        {
            CheckForInteractables();
        }

        // E kich hoat object dang duoc raycast vao.
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (fishingRob != null && fishingRob.TryConsumeInteractInput())
            {
                HidePromptUI();
                return;
            }

            HandleInteractInput();
        }
    }

    private void HandleInteractInput()
    {
        if (!TryGetLookInteractable(out Interactable interactable))
        {
            return;
        }

        // Khi subtitle cua chinh object nay dang hien thi, khong kich hoat lai cho den khi no tu tat.
        if (playerUI != null && playerUI.IsShowingContent(interactable))
        {
            return;
        }

        interactable.BaseInteract(playerUI);
        HidePromptUI();
    }

    private void CheckForInteractables()
    {
        if (TryGetLookInteractable(out Interactable interactable))
        {
            // Khi panel cua chinh object dang mo thi an prompt de UI doan nay khong chong len nhau.
            if (playerUI != null && playerUI.IsShowingContent(interactable))
            {
                HidePromptUI();
            }
            else
            {
                ShowPromptUI(GetInteractablePrompt(interactable));
            }

            return;
        }

        HidePromptUI();
    }

    private bool TryGetLookInteractable(out Interactable interactable)
    {
        interactable = null;

        if (!Physics.Raycast(
            transform.position,
            transform.TransformDirection(Vector3.forward),
            out RaycastHit hit,
            interactionRange))
        {
            return false;
        }

        interactable = hit.transform.GetComponentInParent<Interactable>();
        return interactable != null;
    }

    private string GetInteractablePrompt(Interactable interactable)
    {
        if (interactable == null)
        {
            return string.Empty;
        }

        if (interactable.HasPromptText)
        {
            return interactable.PromptText;
        }

        if (!string.IsNullOrWhiteSpace(defaultInteractionMessage))
        {
            return defaultInteractionMessage;
        }

        return interactable.gameObject.name;
    }

    private void ShowPromptUI(string message)
    {
        if (playerUI != null)
        {
            playerUI.UpdatePrompt(message);
        }
    }

    private void HidePromptUI()
    {
        if (playerUI != null)
        {
            playerUI.UpdatePrompt(string.Empty);
        }
    }
}
