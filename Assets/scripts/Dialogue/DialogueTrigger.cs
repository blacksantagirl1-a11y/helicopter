using UnityEngine;

[DisallowMultipleComponent]
public class DialogueTrigger : Interactable
{
    [Header("Dialogue Event")]
    [SerializeField] private DialogueEventId dialogueEventId = DialogueEventId.None;
    [SerializeField] private bool requireMatchingDay;
    [SerializeField] private DialogueDay requiredDay = DialogueDay.Day1;

    [Header("Trigger Settings")]
    [SerializeField] private bool triggerOnEnter = true;
    [SerializeField] private bool triggerOnce = true;

    private bool hasTriggered;

    public override bool CanInteract => false;
    public override bool HasPromptText => false;
    public override string PromptText => string.Empty;

    private bool HasDialogueEventConfigured => dialogueEventId != DialogueEventId.None;

    private void Reset()
    {
        EnsureTriggerCollider();
    }

    private void Awake()
    {
        EnsureTriggerCollider();
    }

    private void OnValidate()
    {
        EnsureTriggerCollider();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!triggerOnEnter || !CanTriggerFrom(other))
        {
            return;
        }

        TryActivateFromTrigger();
    }

    private void TryActivateFromTrigger()
    {
        if (!IsTriggerAvailable())
        {
            return;
        }

        bool didActivate = false;
        if (HasDialogueEventConfigured)
        {
            didActivate = DialogueController.RequestDialogue(dialogueEventId);
        }
        else if (!DialogueController.IsDialogueActive && HasInteractionContent)
        {
            PlayerUI playerUI = FindFirstObjectByType<PlayerUI>();
            if (playerUI != null)
            {
                playerUI.ShowInteractionContent(this);
                didActivate = true;
            }
        }

        if (didActivate && triggerOnce)
        {
            hasTriggered = true;
        }
    }

    private bool IsTriggerAvailable()
    {
        if (triggerOnce && hasTriggered)
        {
            return false;
        }

        if (requireMatchingDay && DialogueController.GetCurrentDay() != requiredDay)
        {
            return false;
        }

        return HasDialogueEventConfigured || HasInteractionContent;
    }

    private bool CanTriggerFrom(Collider other)
    {
        if (other == null)
        {
            return false;
        }

        return other.GetComponentInParent<PlayerMovement>() != null ||
               other.GetComponentInParent<PickUpScript>() != null ||
               other.CompareTag("Player");
    }

    private void EnsureTriggerCollider()
    {
        Collider triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }
    }
}
