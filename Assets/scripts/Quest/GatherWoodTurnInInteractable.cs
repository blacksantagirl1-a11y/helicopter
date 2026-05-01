using UnityEngine;

[DisallowMultipleComponent]
public sealed class GatherWoodTurnInInteractable : Interactable
{
    [SerializeField] private DailyQuestId questId = DailyQuestId.GatherWood;
    [SerializeField] private InventoryItemDefinition requiredItem;
    [SerializeField]
    [Min(1)]
    private int requiredAmount = 5;
    [SerializeField] private GameObject objectToEnable;
    [SerializeField] private bool consumeItems = true;
    [SerializeField] private bool disableAfterSuccess = true;
    [SerializeField] private string promptText = "Dat go";
    [SerializeField] private string feedbackSpeaker = "Hien";
    [TextArea(1, 3)]
    [SerializeField] private string notEnoughText = "Chua du go de dat vao day.";

    private bool completed;
    private bool isProcessingTurnIn;
    private string feedbackText;

    public override bool CanInteract => !completed && !isProcessingTurnIn && DailyQuestManager.CanTurnInQuest(questId);
    public override bool HasPromptText => CanInteract && !string.IsNullOrWhiteSpace(promptText);
    public override string PromptText => HasPromptText ? promptText : string.Empty;
    public override string DialogueSpeaker => feedbackSpeaker;
    public override string DialogueText => feedbackText;

    public void Configure(InventoryItemDefinition itemDefinition, int amount, GameObject bundleObject)
    {
        requiredItem = itemDefinition;
        requiredAmount = Mathf.Max(1, amount);
        objectToEnable = bundleObject;
    }

    protected override void Interact()
    {
        feedbackText = string.Empty;
        if (!CanInteract)
        {
            return;
        }

        if (requiredItem == null)
        {
            Debug.LogWarning($"GatherWoodTurnInInteractable on '{name}' has no required item assigned.", this);
            feedbackText = notEnoughText;
            return;
        }

        if (!HasEnoughItemsForTurnIn())
        {
            feedbackText = notEnoughText;
            return;
        }

        isProcessingTurnIn = true;
        if (DialogueController.PlayInteractionFade(CompleteTurnInAfterFade))
        {
            return;
        }

        CompleteTurnInAfterFade();
    }

    protected override void PresentInteraction(PlayerUI playerUI)
    {
        if (string.IsNullOrWhiteSpace(feedbackText))
        {
            playerUI?.HideInteractionContent();
            return;
        }

        base.PresentInteraction(playerUI);
    }

    private void OnValidate()
    {
        requiredAmount = Mathf.Max(1, requiredAmount);
    }

    private bool HasEnoughItemsForTurnIn()
    {
        if (requiredItem == null)
        {
            return true;
        }

        PlayerInventory inventory = FindFirstObjectByType<PlayerInventory>();
        return inventory != null && inventory.GetItemCount(requiredItem) >= requiredAmount;
    }

    private void CompleteTurnInAfterFade()
    {
        if (!DailyQuestManager.TryCompleteTurnIn(questId, requiredItem, requiredAmount, consumeItems))
        {
            isProcessingTurnIn = false;
            feedbackText = notEnoughText;
            return;
        }

        if (objectToEnable != null)
        {
            objectToEnable.SetActive(true);
        }

        completed = true;
        isProcessingTurnIn = false;
        if (disableAfterSuccess)
        {
            enabled = false;
        }
    }
}
