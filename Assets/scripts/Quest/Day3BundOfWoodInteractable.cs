using UnityEngine;

[DisallowMultipleComponent]
public sealed class Day3BundOfWoodInteractable : Interactable
{
    [SerializeField] private string promptText = "Kiem tra bo go";
    private bool isProcessingInteraction;

    public override bool CanInteract => !isProcessingInteraction && DailyQuestManager.CanInteractWithDay3BundOfWood();
    public override bool HasPromptText => CanInteract && !string.IsNullOrWhiteSpace(promptText);
    public override string PromptText => HasPromptText ? promptText : string.Empty;

    protected override void Interact()
    {
        if (!CanInteract)
        {
            return;
        }

        isProcessingInteraction = true;
        if (DialogueController.PlayInteractionFade(CompleteBundInteraction))
        {
            return;
        }

        CompleteBundInteraction();
    }

    protected override void PresentInteraction(PlayerUI playerUI)
    {
        playerUI?.HideInteractionContent();
    }

    private void CompleteBundInteraction()
    {
        if (!DailyQuestManager.TryHandleDay3BundOfWoodInteraction())
        {
            isProcessingInteraction = false;
        }
    }
}
