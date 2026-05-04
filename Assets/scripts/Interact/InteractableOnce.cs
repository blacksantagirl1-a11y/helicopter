using UnityEngine;

[DisallowMultipleComponent]
public sealed class InteractableOnce : Interactable
{
    [Header("Dialogue")]
    [SerializeField] private DialogueEventId dialogueEventId = DialogueEventId.Water;
    [SerializeField] private DialogueDay requiredDay = DialogueDay.Day1;

    [Header("Behavior")]
    [SerializeField] private bool triggerOnEnter = true;
    [SerializeField] private bool triggerOnce = true;

    private bool hasTriggered;

    public override bool CanInteract => false;
    public override bool HasPromptText => false;
    public override string PromptText => string.Empty;

    private void OnTriggerEnter(Collider other)
    {
        if (!triggerOnEnter || !CanTriggerFrom(other))
        {
            return;
        }

        TryActivateFromTrigger();
    }

    protected override void PresentInteraction(PlayerUI playerUI)
    {
        if (playerUI != null)
        {
            playerUI.HideInteractionContent();
        }
    }

    private void TryActivateFromTrigger()
    {
        if (!IsTriggerAvailable())
        {
            return;
        }

        bool didRequestDialogue = DialogueController.RequestDialogue(dialogueEventId);
        if (didRequestDialogue && triggerOnce)
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

        return DialogueController.GetCurrentDay() == requiredDay;
    }

    private static bool CanTriggerFrom(Collider other)
    {
        if (other == null)
        {
            return false;
        }

        return other.GetComponentInParent<PlayerMovement>() != null ||
               other.GetComponentInParent<PickUpScript>() != null ||
               other.CompareTag("Player");
    }
}
