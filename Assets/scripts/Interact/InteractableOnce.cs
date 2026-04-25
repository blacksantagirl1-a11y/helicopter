using UnityEngine;

[DisallowMultipleComponent]
public sealed class InteractableOnce : Interactable
{
    [Header("Prompt")]
    [SerializeField] private string interactionPrompt = "Tuong tac";

    [Header("Dialogue")]
    [SerializeField] private DialogueEventId dialogueEventId = DialogueEventId.Water;
    [SerializeField] private DialogueDay requiredDay = DialogueDay.Day1;

    [Header("Behavior")]
    [SerializeField] private bool triggerOnce = true;

    private bool hasTriggered;

    public override bool CanInteract => IsInteractionAvailable();
    public override bool HasPromptText => CanInteract && !string.IsNullOrWhiteSpace(interactionPrompt);
    public override string PromptText => HasPromptText ? interactionPrompt : string.Empty;

    protected override void Interact()
    {
        if (!IsInteractionAvailable())
        {
            return;
        }

        bool didRequestDialogue = DialogueController.RequestDialogue(dialogueEventId);
        if (didRequestDialogue && triggerOnce)
        {
            hasTriggered = true;
        }
    }

    protected override void PresentInteraction(PlayerUI playerUI)
    {
        if (playerUI != null)
        {
            playerUI.HideInteractionContent();
        }
    }

    private bool IsInteractionAvailable()
    {
        if (triggerOnce && hasTriggered)
        {
            return false;
        }

        return DialogueController.GetCurrentDay() == requiredDay;
    }
}
