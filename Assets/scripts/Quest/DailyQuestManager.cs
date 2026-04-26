using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class DailyQuestManager : MonoBehaviour
{
    private const string DefaultDatabaseResourcePath = "Quests/DailyQuestDatabase";
    private const string DefaultHudRootName = "QuestHUDRoot";
    private const string DefaultInfoTextName = "QuestInfoText";
    private const string DefaultTitleTextName = "QuestTitleText";
    private const string DefaultProgressTextName = "QuestProgressText";
    private const string DefaultTimerTextName = "QuestTimerText";

    private static DailyQuestManager instance;

    [Header("Data")]
    [SerializeField] private DailyQuestDatabase database;
    [SerializeField] private string databaseResourcePath = DefaultDatabaseResourcePath;

    [Header("References")]
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private GameObject questHudRoot;
    [SerializeField] private TextMeshProUGUI questTitleText;
    [SerializeField] private TextMeshProUGUI questProgressText;
    [SerializeField] private TextMeshProUGUI questTimerText;

    private DailyQuestDefinition activeQuest;
    private int currentProgress;
    private float remainingTime;
    private bool isQuestActive;
    private bool isWaitingForCompletionDialogue;
    private bool isReloadingScene;
    private PlayerInventory subscribedInventory;

    public static bool IsQuestSystemActive => TryGetInstance() != null;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        ResolveDatabase();
        ResolveReferences();
        RefreshInventorySubscription();
        SetHudVisible(false);
    }

    private void OnEnable()
    {
        ResolveDatabase();
        ResolveReferences();
        RefreshInventorySubscription();
    }

    private void OnDisable()
    {
        UnsubscribeInventory();

        if (instance == this)
        {
            instance = null;
        }
    }

    private void OnValidate()
    {
        ResolveDatabase();
        ResolveReferences();
    }

    private void Update()
    {
        if (!isQuestActive || activeQuest == null || isReloadingScene)
        {
            return;
        }

        if (!isWaitingForCompletionDialogue)
        {
            remainingTime = Mathf.Max(0f, remainingTime - Time.deltaTime);
            if (remainingTime <= 0f)
            {
                FailCurrentDay();
                return;
            }
        }

        RefreshHud();
    }

    public static void TryActivateQuest(DialogueDay day, DailyQuestId questId)
    {
        if (questId == DailyQuestId.None)
        {
            return;
        }

        DailyQuestManager manager = TryGetInstance();
        if (manager == null)
        {
            return;
        }

        manager.StartQuest(day, questId);
    }

    public static void NotifyDialogueFinished(DialogueDay day, DialogueEventId eventId)
    {
        DailyQuestManager manager = TryGetInstance();
        if (manager == null)
        {
            return;
        }

        manager.HandleDialogueFinished(day, eventId);
    }

    public static void ReportInteraction(string interactionKey, int amount = 1)
    {
        if (string.IsNullOrWhiteSpace(interactionKey) || amount <= 0)
        {
            return;
        }

        DailyQuestManager manager = TryGetInstance();
        if (manager == null)
        {
            return;
        }

        manager.HandleInteractionReported(interactionKey, amount);
    }

    private static DailyQuestManager TryGetInstance()
    {
        if (instance != null)
        {
            return instance;
        }

        instance = FindFirstObjectByType<DailyQuestManager>();
        return instance;
    }

    private void StartQuest(DialogueDay day, DailyQuestId questId)
    {
        ResolveDatabase();
        ResolveReferences();
        RefreshInventorySubscription();

        if (database == null)
        {
            Debug.LogWarning("DailyQuestManager could not find a DailyQuestDatabase.", this);
            return;
        }

        if (!database.TryGetQuest(day, questId, out DailyQuestDefinition quest) || quest == null)
        {
            Debug.LogWarning($"DailyQuestManager could not find quest data for {day} / {questId}.", this);
            return;
        }

        activeQuest = quest;
        currentProgress = 0;
        remainingTime = quest.DayDurationSeconds;
        isQuestActive = true;
        isWaitingForCompletionDialogue = false;
        isReloadingScene = false;

        SetHudVisible(true);
        RefreshHud();
    }

    private void HandleDialogueFinished(DialogueDay day, DialogueEventId eventId)
    {
        if (!isQuestActive || activeQuest == null || !isWaitingForCompletionDialogue)
        {
            return;
        }

        if (day != activeQuest.Day || eventId != activeQuest.CompletionDialogueEvent)
        {
            return;
        }

        AdvanceToNextDay();
    }

    private void HandleInteractionReported(string interactionKey, int amount)
    {
        if (!isQuestActive || activeQuest == null || isWaitingForCompletionDialogue)
        {
            return;
        }

        if (activeQuest.ObjectiveType != QuestObjectiveType.InteractionKeyCount)
        {
            return;
        }

        if (!string.Equals(activeQuest.InteractionKey, interactionKey, System.StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        currentProgress = Mathf.Min(activeQuest.RequiredCount, currentProgress + amount);
        RefreshHud();
        TryCompleteQuest();
    }

    private void HandleInventoryItemAdded(InventoryItemDefinition itemDefinition, int amountAdded)
    {
        if (!isQuestActive || activeQuest == null || isWaitingForCompletionDialogue)
        {
            return;
        }

        if (activeQuest.ObjectiveType != QuestObjectiveType.InventoryItemCount ||
            activeQuest.TargetItem == null ||
            itemDefinition != activeQuest.TargetItem)
        {
            return;
        }

        currentProgress = Mathf.Min(activeQuest.RequiredCount, currentProgress + Mathf.Max(0, amountAdded));
        RefreshHud();
        TryCompleteQuest();
    }

    private void TryCompleteQuest()
    {
        if (!isQuestActive || activeQuest == null || currentProgress < activeQuest.RequiredCount)
        {
            return;
        }

        currentProgress = activeQuest.RequiredCount;
        isWaitingForCompletionDialogue = true;
        RefreshHud();

        if (activeQuest.CompletionDialogueEvent == DialogueEventId.None ||
            !DialogueController.RequestDialogue(activeQuest.CompletionDialogueEvent))
        {
            AdvanceToNextDay();
        }
    }

    private void FailCurrentDay()
    {
        if (isReloadingScene)
        {
            return;
        }

        isReloadingScene = true;
        ResetQuestState();
        ReloadCurrentScene();
    }

    private void AdvanceToNextDay()
    {
        if (isReloadingScene)
        {
            return;
        }

        isReloadingScene = true;
        DialogueDay previousDay = activeQuest != null ? activeQuest.Day : DialogueController.GetCurrentDay();
        DialogueDay nextDay = DialogueController.AdvanceDay();
        ResetQuestState();

        if (nextDay == previousDay)
        {
            isReloadingScene = false;
            return;
        }

        ReloadCurrentScene();
    }

    private void ResetQuestState()
    {
        activeQuest = null;
        currentProgress = 0;
        remainingTime = 0f;
        isQuestActive = false;
        isWaitingForCompletionDialogue = false;
        SetHudVisible(false);
    }

    private void ReloadCurrentScene()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (!LoadingManager.LoadScene(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
    }

    private void ResolveDatabase()
    {
        if (database == null && !string.IsNullOrWhiteSpace(databaseResourcePath))
        {
            database = Resources.Load<DailyQuestDatabase>(databaseResourcePath);
        }
    }

    private void ResolveReferences()
    {
        playerInventory ??= FindFirstObjectByType<PlayerInventory>();

        if (questHudRoot == null)
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                Transform hudRoot = canvas.transform.Find(DefaultHudRootName);
                if (hudRoot != null)
                {
                    questHudRoot = hudRoot.gameObject;
                }
            }
        }

        if (questHudRoot != null)
        {
            if (questTitleText == null)
            {
                questTitleText = questHudRoot.transform.Find(DefaultTitleTextName)?.GetComponent<TextMeshProUGUI>();
                questTitleText ??= questHudRoot.transform.Find(DefaultInfoTextName)?.GetComponent<TextMeshProUGUI>();
            }

            if (questProgressText == null)
            {
                questProgressText = questHudRoot.transform.Find(DefaultProgressTextName)?.GetComponent<TextMeshProUGUI>();
            }

            if (questTimerText == null)
            {
                questTimerText = questHudRoot.transform.Find(DefaultTimerTextName)?.GetComponent<TextMeshProUGUI>();
            }
        }
    }

    private void RefreshInventorySubscription()
    {
        if (playerInventory == subscribedInventory)
        {
            return;
        }

        UnsubscribeInventory();

        subscribedInventory = playerInventory;
        if (subscribedInventory != null)
        {
            subscribedInventory.ItemAdded += HandleInventoryItemAdded;
        }
    }

    private void UnsubscribeInventory()
    {
        if (subscribedInventory != null)
        {
            subscribedInventory.ItemAdded -= HandleInventoryItemAdded;
            subscribedInventory = null;
        }
    }

    private void RefreshHud()
    {
        ResolveReferences();
        if (questHudRoot == null || questTitleText == null)
        {
            return;
        }

        if (!isQuestActive || activeQuest == null)
        {
            SetHudVisible(false);
            return;
        }

        SetHudVisible(true);

        string timerLabel = isWaitingForCompletionDialogue
            ? "Dang bao cao nhiem vu..."
            : $"Het ngay: {FormatTime(remainingTime)}";

        if (questProgressText == null || questTimerText == null)
        {
            questTitleText.text = $"{activeQuest.DisplayName}\n{BuildProgressLabel()}\n{timerLabel}";
            return;
        }

        questTitleText.text = activeQuest.DisplayName;
        questProgressText.text = BuildProgressLabel();
        questTimerText.text = timerLabel;
    }

    private string BuildProgressLabel()
    {
        if (activeQuest == null)
        {
            return string.Empty;
        }

        string label = activeQuest.ObjectiveType switch
        {
            QuestObjectiveType.InventoryItemCount when activeQuest.TargetItem != null => activeQuest.TargetItem.DisplayName,
            QuestObjectiveType.InteractionKeyCount => "Tien do",
            _ => "Tien do"
        };

        return $"{label}: {currentProgress}/{activeQuest.RequiredCount}";
    }

    private void SetHudVisible(bool visible)
    {
        if (questHudRoot == null)
        {
            return;
        }

        questHudRoot.SetActive(visible);
        if (!visible)
        {
            if (questTitleText != null)
            {
                questTitleText.text = string.Empty;
            }

            if (questProgressText != null)
            {
                questProgressText.text = string.Empty;
            }

            if (questTimerText != null)
            {
                questTimerText.text = string.Empty;
            }
        }
    }

    private static string FormatTime(float seconds)
    {
        int totalSeconds = Mathf.CeilToInt(Mathf.Max(0f, seconds));
        int minutes = totalSeconds / 60;
        int remainingSeconds = totalSeconds % 60;
        return $"{minutes:00}:{remainingSeconds:00}";
    }
}
