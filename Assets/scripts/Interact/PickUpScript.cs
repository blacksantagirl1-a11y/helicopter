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

        if (fishingRob != null)
        {
            fishingRob.RefreshInteractionAvailability();
        }

        bool hasLookInteractable = TryGetLookInteractable(out Interactable lookInteractable);
        bool fishingIsActive = fishingRob != null && fishingRob.IsFishingActive;

        if (fishingRob != null && fishingRob.ShouldOverrideDefaultInteraction && (fishingIsActive || !hasLookInteractable))
        {
            ShowPromptUI(fishingRob.CurrentPrompt);
        }
        else if (hasLookInteractable)
        {
            ShowInteractablePrompt(lookInteractable);
        }
        else
        {
            HidePromptUI();
        }

        // E kich hoat object dang duoc raycast vao.
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (fishingIsActive && fishingRob.TryConsumeInteractInput())
            {
                HidePromptUI();
                return;
            }

            if (hasLookInteractable)
            {
                HandleInteractInput(lookInteractable);
                return;
            }

            if (fishingRob != null && fishingRob.TryConsumeInteractInput())
            {
                HidePromptUI();
                return;
            }
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

    private void HandleInteractInput(Interactable interactable)
    {
        if (interactable == null)
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
            ShowInteractablePrompt(interactable);

            return;
        }

        HidePromptUI();
    }

    private void ShowInteractablePrompt(Interactable interactable)
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

        Interactable[] candidates = hit.transform.GetComponentsInParent<Interactable>();
        Interactable fallback = null;
        for (int index = 0; index < candidates.Length; index++)
        {
            Interactable candidate = candidates[index];
            if (candidate == null || !candidate.enabled || !candidate.CanInteract)
            {
                continue;
            }

            if (candidate.GetType() != typeof(Interactable))
            {
                interactable = candidate;
                return true;
            }

            fallback ??= candidate;
        }

        interactable = fallback;
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
