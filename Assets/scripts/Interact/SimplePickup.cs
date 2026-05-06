using System.Text;
using UnityEngine;

public class SimplePickup : Interactable
{
    private const string CollectedKeyPrefix = "simplePickup.collected";
    private const string CampaignIdKey = "simplePickup.campaignId";
    private const string LastKnownDayKey = "simplePickup.lastKnownDay";

    [SerializeField] private string questInteractionKey = "trap";
    [SerializeField] private string pickupPromptOverride = "Phá Hủy";

    [SerializeField] private bool persistCollectedAcrossDays = true;

    [Header("Completion Dialogue")]
    [SerializeField] private bool requestDoneDialogueWhenAllCollected = true;
    [SerializeField] private DialogueDay requiredCompletionDay = DialogueDay.Day1;
    [SerializeField] private DialogueEventId completionDialogueEvent = DialogueEventId.DoneRequest;

    private bool isDestroying;
    private string persistentPickupKey;

    public override bool CanInteract => !isDestroying;
    public override string PromptText => pickupPromptOverride;

    private void Awake()
    {
        ApplyPersistentCollectedState();
    }

    private void OnEnable()
    {
        ApplyPersistentCollectedState();
    }

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
        MarkAsCollectedPersistently();
        DailyQuestManager.ReportInteraction(questInteractionKey);
        Destroy(gameObject);

        if (shouldRequestCompletionDialogue)
        {
            DialogueController.RequestDialogue(completionDialogueEvent);
        }
    }

    private void ApplyPersistentCollectedState()
    {
        if (!persistCollectedAcrossDays)
        {
            persistentPickupKey = null;
            return;
        }

        SyncPersistentPickupCampaign();
        persistentPickupKey = BuildPersistentPickupKey();
        if (string.IsNullOrEmpty(persistentPickupKey) || PlayerPrefs.GetInt(persistentPickupKey, 0) != 1)
        {
            return;
        }

        gameObject.SetActive(false);
    }

    private void MarkAsCollectedPersistently()
    {
        if (!persistCollectedAcrossDays)
        {
            return;
        }

        if (string.IsNullOrEmpty(persistentPickupKey))
        {
            SyncPersistentPickupCampaign();
            persistentPickupKey = BuildPersistentPickupKey();
        }

        if (string.IsNullOrEmpty(persistentPickupKey))
        {
            return;
        }

        PlayerPrefs.SetInt(persistentPickupKey, 1);
        PlayerPrefs.Save();
    }

    private static void SyncPersistentPickupCampaign()
    {
        int currentDay = Mathf.Max((int)DialogueDay.Day1, (int)DialogueController.GetCurrentDay());
        int lastKnownDay = PlayerPrefs.GetInt(LastKnownDayKey, currentDay);

        if (currentDay == (int)DialogueDay.Day1 && lastKnownDay > (int)DialogueDay.Day1)
        {
            int nextCampaignId = PlayerPrefs.GetInt(CampaignIdKey, 0) + 1;
            PlayerPrefs.SetInt(CampaignIdKey, nextCampaignId);
        }

        if (lastKnownDay != currentDay)
        {
            PlayerPrefs.SetInt(LastKnownDayKey, currentDay);
        }

        PlayerPrefs.Save();
    }

    private string BuildPersistentPickupKey()
    {
        string sceneIdentifier = gameObject.scene.path;
        if (string.IsNullOrWhiteSpace(sceneIdentifier))
        {
            sceneIdentifier = gameObject.scene.name;
        }

        if (string.IsNullOrWhiteSpace(sceneIdentifier))
        {
            return null;
        }

        StringBuilder hierarchyPathBuilder = new StringBuilder(sceneIdentifier);
        hierarchyPathBuilder.Append('|');
        hierarchyPathBuilder.Append(GetHierarchyPath(transform));

        int campaignId = PlayerPrefs.GetInt(CampaignIdKey, 0);
        return $"{CollectedKeyPrefix}.{campaignId}.{hierarchyPathBuilder}";
    }

    private static string GetHierarchyPath(Transform current)
    {
        if (current == null)
        {
            return string.Empty;
        }

        if (current.parent == null)
        {
            return current.name;
        }

        return $"{GetHierarchyPath(current.parent)}/{current.name}";
    }
}
