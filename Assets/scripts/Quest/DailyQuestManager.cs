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
    private const string GatherWoodTurnInObjectName = "Carpets2";
    private const string GatherWoodBundleObjectName = "BundOfWood";
    private const string GatherWoodBundlePlacedKey = "quest.gatherWood.bundlePlaced";
    private const string Day3CarpetsObjectName = "Carpets";
    private const string Day3BedObjectName = "bed";
    private const string Day3CarpetsShownKey = "quest.day3.carpetsShown";
    private const string Day4BlockTriggerObjectName = "BlockTriggerDay4";
    private const string Day4DialogueTriggerObjectName = "DialogueTriggerDay4";
    private const DialogueDay BundleAndBedAdvanceDay = DialogueDay.Day4;

    private static DailyQuestManager instance;

    [Header("Data")]
    [SerializeField] private DailyQuestDatabase database;
    [SerializeField] private string databaseResourcePath = DefaultDatabaseResourcePath;

    [Header("References")]
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private GameObject questHudRoot;
    [SerializeField] private TextMeshProUGUI questTitleText;
    [SerializeField] private TextMeshProUGUI questProgressText;

    private DailyQuestDefinition activeQuest;
    private int currentProgress;
    private bool isQuestActive;
    private bool isWaitingForCompletionDialogue;
    private bool isWaitingForTurnIn;
    private bool isReloadingScene;
    private DialogueEventId pendingCompletionDialogueEvent = DialogueEventId.None;
    private bool shouldAdvanceAfterPendingDialogue;
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
        ApplyPersistentSceneState();
        SetHudVisible(false);
    }

    private void OnEnable()
    {
        ResolveDatabase();
        ResolveReferences();
        RefreshInventorySubscription();
        ApplyPersistentSceneState();
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

    public static bool CanTurnInQuest(DailyQuestId questId)
    {
        DailyQuestManager manager = TryGetInstance();
        return manager != null &&
            manager.isQuestActive &&
            manager.activeQuest != null &&
            manager.activeQuest.QuestId == questId &&
            manager.isWaitingForTurnIn &&
            !manager.isWaitingForCompletionDialogue &&
            !manager.isReloadingScene;
    }

    public static bool TryCompleteTurnIn(
        DailyQuestId questId,
        InventoryItemDefinition requiredItem,
        int requiredAmount,
        bool consumeItems)
    {
        DailyQuestManager manager = TryGetInstance();
        if (manager == null)
        {
            return false;
        }

        return manager.CompleteTurnIn(questId, requiredItem, requiredAmount, consumeItems);
    }

    public static bool CanInteractWithDay3BundOfWood()
    {
        return DialogueController.GetCurrentDay() == BundleAndBedAdvanceDay &&
            !DialogueController.IsDialogueActive &&
            !IsDay3CarpetsShown();
    }

    public static bool TryHandleDay3BundOfWoodInteraction()
    {
        DailyQuestManager manager = TryGetInstance();
        if (manager == null || !CanInteractWithDay3BundOfWood())
        {
            return false;
        }

        SetDay3CarpetsShown(true);
        manager.ApplyPersistentSceneState();
        DialogueController.RequestDialogue(DialogueEventId.InvestigationStart);
        return true;
    }

    public static bool CanAdvanceFromDay3Bed()
    {
        return DialogueController.GetCurrentDay() == BundleAndBedAdvanceDay &&
            !DialogueController.IsDialogueActive &&
            IsDay3CarpetsShown();
    }

    public static bool TryAdvanceFromDay3Bed()
    {
        DailyQuestManager manager = TryGetInstance();
        if (manager == null || !CanAdvanceFromDay3Bed())
        {
            return false;
        }

        return manager.AdvanceDayFromBed();
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
        currentProgress = GetInitialProgress(quest);
        isQuestActive = true;
        isWaitingForCompletionDialogue = false;
        isWaitingForTurnIn = false;
        isReloadingScene = false;
        pendingCompletionDialogueEvent = DialogueEventId.None;
        shouldAdvanceAfterPendingDialogue = false;

        PrepareTurnInScene(quest);

        SetHudVisible(true);
        RefreshHud();
        TryCompleteQuest();
    }

    private void HandleDialogueFinished(DialogueDay day, DialogueEventId eventId)
    {
        if (!isQuestActive ||
            activeQuest == null ||
            !isWaitingForCompletionDialogue ||
            pendingCompletionDialogueEvent == DialogueEventId.None)
        {
            return;
        }

        if (day != activeQuest.Day || eventId != pendingCompletionDialogueEvent)
        {
            return;
        }

        FinishPendingCompletionDialogue();
    }

    private void HandleInteractionReported(string interactionKey, int amount)
    {
        if (!isQuestActive || activeQuest == null || isWaitingForCompletionDialogue || isWaitingForTurnIn)
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
        if (!isQuestActive || activeQuest == null || isWaitingForCompletionDialogue || isWaitingForTurnIn)
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
        BeginCompletionDialogue(
            activeQuest.CompletionDialogueEvent,
            !activeQuest.RequiresTurnInAfterCompletionDialogue);
    }

    private bool CompleteTurnIn(
        DailyQuestId questId,
        InventoryItemDefinition requiredItem,
        int requiredAmount,
        bool consumeItems)
    {
        if (!CanCompleteTurnIn(questId, requiredItem, requiredAmount, out PlayerInventory inventory))
        {
            return false;
        }

        requiredAmount = Mathf.Max(1, requiredAmount);
        if (consumeItems && requiredItem != null && !inventory.TryRemoveItem(requiredItem, requiredAmount))
        {
            return false;
        }

        currentProgress = activeQuest.RequiredCount;
        if (activeQuest.QuestId == DailyQuestId.GatherWood)
        {
            SetGatherWoodBundlePlaced(true);
        }

        BeginCompletionDialogue(activeQuest.TurnInCompletionDialogueEvent, true);
        return true;
    }

    private bool CanCompleteTurnIn(
        DailyQuestId questId,
        InventoryItemDefinition requiredItem,
        int requiredAmount,
        out PlayerInventory inventory)
    {
        inventory = null;
        if (!isQuestActive ||
            activeQuest == null ||
            activeQuest.QuestId != questId ||
            !isWaitingForTurnIn ||
            isWaitingForCompletionDialogue ||
            isReloadingScene)
        {
            return false;
        }

        ResolveReferences();
        RefreshInventorySubscription();
        inventory = playerInventory;
        if (requiredItem == null)
        {
            return true;
        }

        requiredAmount = Mathf.Max(1, requiredAmount);
        return inventory != null && inventory.GetItemCount(requiredItem) >= requiredAmount;
    }

    private void BeginCompletionDialogue(DialogueEventId eventId, bool advanceAfterDialogue)
    {
        isWaitingForTurnIn = false;
        isWaitingForCompletionDialogue = true;
        pendingCompletionDialogueEvent = eventId;
        shouldAdvanceAfterPendingDialogue = advanceAfterDialogue;
        RefreshHud();

        if (eventId == DialogueEventId.None || !DialogueController.RequestDialogue(eventId))
        {
            FinishPendingCompletionDialogue();
        }
    }

    private void FinishPendingCompletionDialogue()
    {
        bool shouldAdvance = shouldAdvanceAfterPendingDialogue;
        isWaitingForCompletionDialogue = false;
        pendingCompletionDialogueEvent = DialogueEventId.None;
        shouldAdvanceAfterPendingDialogue = false;

        if (shouldAdvance)
        {
            AdvanceToNextDay();
            return;
        }

        isWaitingForTurnIn = true;
        EnsureGatherWoodTurnInInteractable();
        RefreshHud();
    }

    private void PrepareTurnInScene(DailyQuestDefinition quest)
    {
        if (quest == null || !quest.RequiresTurnInAfterCompletionDialogue)
        {
            return;
        }

        GameObject bundle = FindSceneObjectByName(GatherWoodBundleObjectName, true);
        if (bundle != null)
        {
            bundle.SetActive(false);
        }
    }

    private void ApplyPersistentSceneState()
    {
        DialogueDay currentDay = DialogueController.GetCurrentDay();
        ApplyGatherWoodBundleState(currentDay);
        ApplyDay3CarpetsState(currentDay);
        ApplyDay4TriggerState(currentDay);
        EnsureDay3Interactables(currentDay);
    }

    private static void ApplyGatherWoodBundleState(DialogueDay currentDay)
    {
        GameObject bundle = FindSceneObjectByName(GatherWoodBundleObjectName, true);
        if (bundle == null)
        {
            return;
        }

        if (currentDay == DialogueDay.Day1 && IsGatherWoodBundlePlaced())
        {
            SetGatherWoodBundlePlaced(false);
        }

        bool shouldShowGatherWoodBundle =
            IsGatherWoodBundlePlaced() ||
            (int)currentDay > (int)DialogueDay.Day2;
        bundle.SetActive(shouldShowGatherWoodBundle);
    }

    private static void ApplyDay3CarpetsState(DialogueDay currentDay)
    {
        if ((int)currentDay < (int)BundleAndBedAdvanceDay && IsDay3CarpetsShown())
        {
            SetDay3CarpetsShown(false);
        }

        GameObject carpets = FindSceneObjectByName(Day3CarpetsObjectName, true);
        if (carpets != null)
        {
            bool shouldShowCarpets =
                (int)currentDay >= (int)BundleAndBedAdvanceDay &&
                IsDay3CarpetsShown();
            carpets.SetActive(shouldShowCarpets);
        }
    }

    private static void ApplyDay4TriggerState(DialogueDay currentDay)
    {
        SetSceneObjectActive(
            Day4BlockTriggerObjectName,
            currentDay == BundleAndBedAdvanceDay);

        SetSceneObjectActive(
            Day4DialogueTriggerObjectName,
            currentDay == BundleAndBedAdvanceDay && IsDay3CarpetsShown());
    }

    private static void EnsureDay3Interactables(DialogueDay currentDay)
    {
        if (currentDay != BundleAndBedAdvanceDay)
        {
            return;
        }

        GameObject bundle = FindSceneObjectByName(GatherWoodBundleObjectName, true);
        if (bundle != null)
        {
            bundle.SetActive(true);
            EnsureCollider(bundle);
            EnsureComponent<Day3BundOfWoodInteractable>(bundle);
        }

        GameObject bed = FindSceneObjectByName(Day3BedObjectName, false);
        if (bed != null)
        {
            EnsureComponent<Day3BedAdvanceInteractable>(bed);
        }
    }

    private static bool IsGatherWoodBundlePlaced()
    {
        return PlayerPrefs.GetInt(GatherWoodBundlePlacedKey, 0) == 1;
    }

    private static void SetGatherWoodBundlePlaced(bool isPlaced)
    {
        if (isPlaced)
        {
            PlayerPrefs.SetInt(GatherWoodBundlePlacedKey, 1);
        }
        else
        {
            PlayerPrefs.DeleteKey(GatherWoodBundlePlacedKey);
        }

        PlayerPrefs.Save();
    }

    private static bool IsDay3CarpetsShown()
    {
        return PlayerPrefs.GetInt(Day3CarpetsShownKey, 0) == 1;
    }

    private static void SetDay3CarpetsShown(bool isShown)
    {
        if (isShown)
        {
            PlayerPrefs.SetInt(Day3CarpetsShownKey, 1);
        }
        else
        {
            PlayerPrefs.DeleteKey(Day3CarpetsShownKey);
        }

        PlayerPrefs.Save();
    }

    private void EnsureGatherWoodTurnInInteractable()
    {
        if (activeQuest == null ||
            activeQuest.QuestId != DailyQuestId.GatherWood ||
            !activeQuest.RequiresTurnInAfterCompletionDialogue)
        {
            return;
        }

        GameObject turnInObject = FindSceneObjectByName(GatherWoodTurnInObjectName, false);
        if (turnInObject == null)
        {
            Debug.LogWarning($"DailyQuestManager could not find '{GatherWoodTurnInObjectName}' for GatherWood turn-in.", this);
            return;
        }

        GatherWoodTurnInInteractable turnIn = turnInObject.GetComponent<GatherWoodTurnInInteractable>();
        if (turnIn == null)
        {
            turnIn = turnInObject.AddComponent<GatherWoodTurnInInteractable>();
        }

        EnsureCollider(turnInObject);

        GameObject bundle = FindSceneObjectByName(GatherWoodBundleObjectName, true);
        turnIn.Configure(activeQuest.TargetItem, activeQuest.RequiredCount, bundle);
    }

    private static T EnsureComponent<T>(GameObject target) where T : Component
    {
        if (target == null)
        {
            return null;
        }

        T component = target.GetComponent<T>();
        if (component == null)
        {
            component = target.AddComponent<T>();
        }

        return component;
    }

    private static void EnsureCollider(GameObject target)
    {
        if (target == null || target.GetComponentInChildren<Collider>() != null)
        {
            return;
        }

        MeshFilter meshFilter = target.GetComponent<MeshFilter>();
        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            MeshCollider meshCollider = target.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = meshFilter.sharedMesh;
            return;
        }

        BoxCollider boxCollider = target.AddComponent<BoxCollider>();
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
        {
            return;
        }

        Bounds bounds = renderers[0].bounds;
        for (int index = 1; index < renderers.Length; index++)
        {
            bounds.Encapsulate(renderers[index].bounds);
        }

        Vector3 lossyScale = target.transform.lossyScale;
        boxCollider.center = target.transform.InverseTransformPoint(bounds.center);
        boxCollider.size = new Vector3(
            SafeInverseScale(bounds.size.x, lossyScale.x),
            SafeInverseScale(bounds.size.y, lossyScale.y),
            SafeInverseScale(bounds.size.z, lossyScale.z));
    }

    private static float SafeInverseScale(float worldSize, float scale)
    {
        float absoluteScale = Mathf.Abs(scale);
        return absoluteScale > 0.0001f ? worldSize / absoluteScale : worldSize;
    }

    private static GameObject FindSceneObjectByName(string objectName, bool includeInactive)
    {
        if (string.IsNullOrWhiteSpace(objectName))
        {
            return null;
        }

        FindObjectsInactive inactiveMode = includeInactive
            ? FindObjectsInactive.Include
            : FindObjectsInactive.Exclude;
        Transform[] transforms = FindObjectsByType<Transform>(inactiveMode, FindObjectsSortMode.None);
        for (int index = 0; index < transforms.Length; index++)
        {
            Transform candidate = transforms[index];
            if (candidate != null && candidate.name == objectName)
            {
                return candidate.gameObject;
            }
        }

        return null;
    }

    private static void SetSceneObjectActive(string objectName, bool isActive)
    {
        GameObject sceneObject = FindSceneObjectByName(objectName, true);
        if (sceneObject != null)
        {
            sceneObject.SetActive(isActive);
        }
    }

    private bool AdvanceDayFromBed()
    {
        if (isReloadingScene)
        {
            return false;
        }

        isReloadingScene = true;
        DialogueDay previousDay = DialogueController.GetCurrentDay();
        DialogueDay nextDay = DialogueController.AdvanceDay();
        ResetQuestState();

        if (nextDay == previousDay)
        {
            isReloadingScene = false;
            ApplyPersistentSceneState();
            return true;
        }

        ReloadCurrentScene();
        return true;
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
        isQuestActive = false;
        isWaitingForCompletionDialogue = false;
        isWaitingForTurnIn = false;
        pendingCompletionDialogueEvent = DialogueEventId.None;
        shouldAdvanceAfterPendingDialogue = false;
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

    private int GetInitialProgress(DailyQuestDefinition quest)
    {
        if (quest == null ||
            quest.ObjectiveType != QuestObjectiveType.InventoryItemCount ||
            quest.TargetItem == null)
        {
            return 0;
        }

        ResolveReferences();
        return playerInventory != null
            ? Mathf.Min(quest.RequiredCount, playerInventory.GetItemCount(quest.TargetItem))
            : 0;
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

        string progressLabel = BuildProgressLabel();
        string instructionLabel = BuildInstructionLabel();
        if (!string.IsNullOrWhiteSpace(instructionLabel))
        {
            progressLabel = $"{progressLabel}\n{instructionLabel}";
        }

        string statusLabel = BuildStatusLabel();
        if (!string.IsNullOrWhiteSpace(statusLabel))
        {
            progressLabel = $"{progressLabel}\n{statusLabel}";
        }

        if (questProgressText == null)
        {
            questTitleText.text = $"{BuildDisplayName()}\n{progressLabel}";
            return;
        }

        questTitleText.text = BuildDisplayName();
        questProgressText.text = progressLabel;
    }

    private string BuildDisplayName()
    {
        if (activeQuest == null)
        {
            return string.Empty;
        }

        return ShouldShowTurnInDisplayName()
            ? activeQuest.TurnInDisplayName
            : activeQuest.DisplayName;
    }

    private bool ShouldShowTurnInDisplayName()
    {
        return activeQuest != null &&
            activeQuest.RequiresTurnInAfterCompletionDialogue &&
            currentProgress >= activeQuest.RequiredCount &&
            (isWaitingForCompletionDialogue || isWaitingForTurnIn);
    }

    private bool ShouldShowTurnInInstruction()
    {
        return activeQuest != null &&
            activeQuest.RequiresTurnInAfterCompletionDialogue &&
            currentProgress >= activeQuest.RequiredCount &&
            (isWaitingForCompletionDialogue || isWaitingForTurnIn);
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
            QuestObjectiveType.InteractionKeyCount => "Tiến độ",
            _ => "Tiến độ"
        };

        return $"{label}: {currentProgress}/{activeQuest.RequiredCount}";
    }

    private string BuildInstructionLabel()
    {
        if (activeQuest == null)
        {
            return string.Empty;
        }

        if (ShouldShowTurnInInstruction())
        {
            if (!string.IsNullOrWhiteSpace(activeQuest.TurnInInstructionText))
            {
                return $"H\u01b0\u1edbng d\u1eabn: {activeQuest.TurnInInstructionText}";
            }

            return "H\u01b0\u1edbng d\u1eabn: Mang v\u1eadt ph\u1ea9m \u0111\u1ebfn \u0111i\u1ec3m b\u00e0n giao \u0111\u1ec3 ho\u00e0n t\u1ea5t nhi\u1ec7m v\u1ee5.";
        }

        if (!string.IsNullOrWhiteSpace(activeQuest.InstructionText))
        {
            return $"H\u01b0\u1edbng d\u1eabn: {activeQuest.InstructionText}";
        }

        return activeQuest.ObjectiveType switch
        {
            QuestObjectiveType.InventoryItemCount when activeQuest.TargetItem != null =>
                $"H\u01b0\u1edbng d\u1eabn: Thu th\u1eadp {activeQuest.TargetItem.DisplayName} cho \u0111\u1ee7 s\u1ed1 l\u01b0\u1ee3ng.",
            QuestObjectiveType.InteractionKeyCount =>
                "H\u01b0\u1edbng d\u1eabn: T\u01b0\u01a1ng t\u00e1c v\u1edbi c\u00e1c m\u1ee5c ti\u00eau trong khu v\u1ef1c \u0111\u1ec3 t\u0103ng ti\u1ebfn \u0111\u1ed9.",
            _ => string.Empty
        };
    }

    private string BuildStatusLabel()
    {
        if (isWaitingForCompletionDialogue)
        {
            return "\u0110ang b\u00e1o c\u00e1o nhi\u1ec7m v\u1ee5...";
        }

        if (isWaitingForTurnIn)
        {
            return "Ch\u1edd b\u00e0n giao nhi\u1ec7m v\u1ee5.";
        }

        return string.Empty;
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
        }
    }
}
