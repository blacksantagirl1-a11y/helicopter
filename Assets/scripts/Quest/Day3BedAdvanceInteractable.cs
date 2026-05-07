using UnityEngine;

[DisallowMultipleComponent]
public sealed class Day3BedAdvanceInteractable : Interactable
{
    [SerializeField] private string promptText = "Nghỉ ngơi";

    public override bool CanInteract => DailyQuestManager.CanAdvanceFromDay3Bed();
    public override bool HasPromptText => CanInteract && !string.IsNullOrWhiteSpace(promptText);
    public override string PromptText => HasPromptText ? promptText : string.Empty;

    protected override void Interact()
    {
        DailyQuestManager.TryAdvanceFromDay3Bed();
    }

    protected override void PresentInteraction(PlayerUI playerUI)
    {
        playerUI?.HideInteractionContent();
    }
}
