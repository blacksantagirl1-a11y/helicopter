using UnityEngine;

public class SimplePickup : Interactable
{
    [SerializeField] private string questInteractionKey = "trap";
    [SerializeField] private string pickupPromptOverride = "Phá Hủy";

    [Header("Completion Dialogue")]
    [SerializeField] private bool requestDoneDialogueWhenAllCollected = true;
    [SerializeField] private DialogueDay requiredCompletionDay = DialogueDay.Day1;
    [SerializeField] private DialogueEventId completionDialogueEvent = DialogueEventId.DoneRequest;

    public override string PromptText => pickupPromptOverride;

    protected override void Interact()
    {
        DailyQuestManager.ReportInteraction(questInteractionKey);
        bool shouldRequestCompletionDialogue = !DailyQuestManager.IsQuestSystemActive && ShouldRequestCompletionDialogue();
        Destroy(gameObject);

        if (shouldRequestCompletionDialogue)
        {
            DialogueController.RequestDialogue(completionDialogueEvent);
        }
    }

    protected override void PresentInteraction(PlayerUI playerUI)
    {
    }

    private bool ShouldRequestCompletionDialogue()
    {
        if (!requestDoneDialogueWhenAllCollected ||
            completionDialogueEvent == DialogueEventId.None ||
            DialogueController.GetCurrentDay() != requiredCompletionDay)
        {
            return false;
        }

        SimplePickup[] activePickups = FindObjectsByType<SimplePickup>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        int pickupsInCurrentScene = 0;
        for (int index = 0; index < activePickups.Length; index++)
        {
            SimplePickup pickup = activePickups[index];
            if (pickup == null || pickup.gameObject.scene != gameObject.scene)
            {
                continue;
            }

            pickupsInCurrentScene++;
            if (pickupsInCurrentScene > 1)
            {
                return false;
            }
        }

        return pickupsInCurrentScene == 1;
    }
}
