using UnityEngine;

public class SimplePickup : Interactable
{
    [SerializeField] private string questInteractionKey = "trap";
    [SerializeField] private string pickupPromptOverride = "Phá Hủy";

    [Header("Completion Dialogue")]
    [SerializeField] private bool requestDoneDialogueWhenAllCollected = true;
    [SerializeField] private DialogueDay requiredCompletionDay = DialogueDay.Day1;
    [SerializeField] private DialogueEventId completionDialogueEvent = DialogueEventId.DoneRequest;

    private bool isDestroying;

    public override bool CanInteract => !isDestroying;
    public override string PromptText => pickupPromptOverride;

    protected override void Interact()
    {
        bool shouldRequestCompletionDialogue = !DailyQuestManager.IsQuestSystemActive && ShouldRequestCompletionDialogue();
        bool shouldPlayFade = string.Equals(
            questInteractionKey,
            "trap",
            System.StringComparison.OrdinalIgnoreCase);

        if (!shouldPlayFade)
        {
            CompletePickupInteraction(shouldRequestCompletionDialogue);
            return;
        }

        isDestroying = true;
        if (DialogueController.PlayInteractionFade(() => CompletePickupInteraction(shouldRequestCompletionDialogue)))
        {
            return;
        }

        CompletePickupInteraction(shouldRequestCompletionDialogue);
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

    private void CompletePickupInteraction(bool shouldRequestCompletionDialogue)
    {
        DailyQuestManager.ReportInteraction(questInteractionKey);
        PlayPickUpSound();
        Destroy(gameObject);

        if (shouldRequestCompletionDialogue)
        {
            DialogueController.RequestDialogue(completionDialogueEvent);
        }
    }

    private static void PlayPickUpSound()
    {
        SoundManager soundManager = ResolveSoundManager();
        PlayOneShot(soundManager != null ? soundManager.pickUpSource : null);
    }

    private static SoundManager ResolveSoundManager()
    {
        return SoundManager.Instance != null
            ? SoundManager.Instance
            : FindFirstObjectByType<SoundManager>();
    }

    private static void PlayOneShot(AudioSource audioSource)
    {
        if (audioSource == null)
        {
            return;
        }

        if (audioSource.clip != null)
        {
            audioSource.PlayOneShot(audioSource.clip);
            return;
        }

        audioSource.Play();
    }
}
