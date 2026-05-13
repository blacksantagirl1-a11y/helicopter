using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public class DailyQuestManager : MonoBehaviour
{
    private enum Day5SurvivalStage
    {
        Inactive,
        CatchFish,
        GatherWood,
        PlaceCampfire,
        CookFood,
        EatFood,
        Completed
    }

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
    private const string Day5EventRootObjectName = "EVENTDAY5";
    private const string Day5DataCubeObjectName = "Day5DataCube";
    private const string Day5ComputerObjectName = "Day5Computer";
    private const string Day5DataCubeAppearedKey = "quest.day5.dataCubeAppeared";
    private const string Day5DataCubeOpenedKey = "quest.day5.dataCubeOpened";
    private const string Day5TableObjectName = "table";
    private const string Day5FishItemId = "river_fish";
    private const string Day5WoodItemId = "wood_log";
    private const string Day5ComputerPrefabAssetPath = "Assets/model/computer/source/PC/PC/PC.prefab";
    private const string Day5ComputerPrefabFallbackAssetPath = "Assets/model/Computer/source/PC/PC/PC.prefab";
    private const int Day5RequiredFish = 7;
    private const int Day5RequiredWood = 5;
    private const int Day5RequiredCookedFood = 3;
    private const int Day5RequiredEatenFood = 1;

    public const string Day5CampfirePlacedInteractionKey = "day5_campfire_placed";
    public const string Day5CookedFoodInteractionKey = "day5_cooked_food";
    public const string Day5AteFoodInteractionKey = "day5_ate_food";

    private static readonly string[] Day5ComputerObjectNames =
    {
        Day5ComputerObjectName,
        "PC",
        "Computer",
        "computer"
    };

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
    private Day5SurvivalStage day5Stage = Day5SurvivalStage.Inactive;
    private int day5FishCount;
    private int day5WoodCount;
    private int day5CookedFoodCount;
    private int day5EatenFoodCount;
    private bool day5FishReady;
    private bool day5WoodReady;
    private bool day5FishDialoguePlayed;
    private bool day5WoodDialoguePlayed;
    private bool day5CampfirePlaced;
    private bool day5CampfireDialoguePlayed;
    private bool day5CookingCompleteDialoguePending;
    private bool day5CookingCompleteDialoguePlayed;
    private bool day5GunshotFollowupRequested;
    private bool hasPreparedDay5DataPackageThisSession;
    private Coroutine day5CookingCompleteDialogueRoutine;
    private Coroutine day5EatingInteractionRestoreRoutine;

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
        CancelDay5CookingCompleteDialogueRoutine();
        CancelDay5EatingInteractionRestoreRoutine();
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

    public static bool CanOpenDay5DataCube()
    {
        return DialogueController.GetCurrentDay() == DialogueDay.Day5 &&
            IsDay5DataCubeAppeared() &&
            !IsDay5DataCubeOpened() &&
            !DialogueController.IsDialogueActive;
    }

    public static bool TryOpenDay5DataCube()
    {
        DailyQuestManager manager = TryGetInstance();
        return manager != null && manager.OpenDay5DataCube();
    }

    public static void ResetDay5DataPackageForReplay()
    {
        ClearDay5DataPackageState();
        if (!Application.isPlaying)
        {
            return;
        }

        DailyQuestManager manager = TryGetInstance();
        if (manager != null)
        {
            manager.hasPreparedDay5DataPackageThisSession = false;
            manager.ApplyPersistentSceneState();
        }
    }

    public static void NotifyFishingModeExited()
    {
        DailyQuestManager manager = TryGetInstance();
        if (manager != null)
        {
            manager.HandleFishingModeExited();
        }
    }

    public static void NotifyCookingMiniGameClosed()
    {
        DailyQuestManager manager = TryGetInstance();
        if (manager != null)
        {
            manager.HandleCookingMiniGameClosed();
        }
    }

    public static bool ShouldPrioritizeDay5CookingOverEating()
    {
        DailyQuestManager manager = TryGetInstance();
        return manager != null && manager.IsDay5CookingObjectiveActive();
    }

    public static bool CanEatDay5CookedFood()
    {
        DailyQuestManager manager = TryGetInstance();
        return manager != null && manager.IsDay5EatingObjectiveActive();
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

        if (quest.ObjectiveType == QuestObjectiveType.Day5Survival)
        {
            BeginDay5SurvivalQuest();
        }
        else
        {
            ResetDay5SurvivalState();
        }

        PrepareTurnInScene(quest);

        SetHudVisible(true);
        RefreshHud();
        TryCompleteQuest();
    }

    private void HandleDialogueFinished(DialogueDay day, DialogueEventId eventId)
    {
        HandleDay5StoryDialogueFinished(day, eventId);

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

        if (activeQuest.ObjectiveType == QuestObjectiveType.Day5Survival)
        {
            HandleDay5InteractionReported(interactionKey, amount);
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

        if (activeQuest.ObjectiveType == QuestObjectiveType.Day5Survival)
        {
            HandleDay5InventoryItemAdded(itemDefinition, amountAdded);
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
        if (activeQuest != null && activeQuest.ObjectiveType == QuestObjectiveType.Day5Survival)
        {
            TryCompleteDay5SurvivalQuest();
            return;
        }

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

    private void HandleDay5StoryDialogueFinished(DialogueDay day, DialogueEventId eventId)
    {
        if (day != DialogueDay.Day5)
        {
            return;
        }

        if (eventId == DialogueEventId.IntroWakeUp)
        {
            hasPreparedDay5DataPackageThisSession = true;
            ClearDay5DataPackageState();
            SetDay5DataCubeAppeared(true);
            ApplyDay5DataPackageState(day);
            DialogueController.RequestDialogue(DialogueEventId.Day5DataCubeAppears);
            return;
        }

        if (eventId == DialogueEventId.Day5CookingComplete && !day5GunshotFollowupRequested)
        {
            day5GunshotFollowupRequested = true;
            ReSoundManager.Resolve()?.PlaySound2D(SoundIds.GunShot);
            if (!DialogueController.RequestDialogue(DialogueEventId.Day5AfterGunshot))
            {
                ScheduleDay5EatingInteractionRestore();
                RefreshDay5Stage();
                RefreshHud();
            }

            return;
        }

        if (eventId == DialogueEventId.Day5AfterGunshot)
        {
            ScheduleDay5EatingInteractionRestore();
            RefreshDay5Stage();
            RefreshHud();
        }
    }

    private void BeginDay5SurvivalQuest()
    {
        day5Stage = Day5SurvivalStage.CatchFish;
        day5FishCount = 0;
        day5WoodCount = 0;
        day5CookedFoodCount = 0;
        day5EatenFoodCount = 0;
        day5FishReady = false;
        day5WoodReady = false;
        day5FishDialoguePlayed = false;
        day5WoodDialoguePlayed = false;
        day5CampfirePlaced = false;
        day5CampfireDialoguePlayed = false;
        day5CookingCompleteDialoguePending = false;
        day5CookingCompleteDialoguePlayed = false;
        day5GunshotFollowupRequested = false;
        CancelDay5CookingCompleteDialogueRoutine();
        CancelDay5EatingInteractionRestoreRoutine();
        currentProgress = 0;
        SyncDay5InventoryProgress(false, false);
        RefreshDay5Stage();
    }

    private void ResetDay5SurvivalState()
    {
        day5Stage = Day5SurvivalStage.Inactive;
        day5FishCount = 0;
        day5WoodCount = 0;
        day5CookedFoodCount = 0;
        day5EatenFoodCount = 0;
        day5FishReady = false;
        day5WoodReady = false;
        day5FishDialoguePlayed = false;
        day5WoodDialoguePlayed = false;
        day5CampfirePlaced = false;
        day5CampfireDialoguePlayed = false;
        day5CookingCompleteDialoguePending = false;
        day5CookingCompleteDialoguePlayed = false;
        day5GunshotFollowupRequested = false;
        hasPreparedDay5DataPackageThisSession = false;
        CancelDay5CookingCompleteDialogueRoutine();
        CancelDay5EatingInteractionRestoreRoutine();
    }

    private void HandleDay5InventoryItemAdded(InventoryItemDefinition itemDefinition, int amountAdded)
    {
        if (itemDefinition == null || amountAdded <= 0)
        {
            return;
        }

        bool addedWood = MatchesItemId(itemDefinition, Day5WoodItemId);
        SyncDay5InventoryProgress(false, addedWood);
        RefreshDay5Stage();
        RefreshHud();
    }

    private void HandleDay5InteractionReported(string interactionKey, int amount)
    {
        amount = Mathf.Max(1, amount);

        if (string.Equals(interactionKey, Day5CampfirePlacedInteractionKey, System.StringComparison.OrdinalIgnoreCase))
        {
            day5CampfirePlaced = true;
            if (!day5CampfireDialoguePlayed)
            {
                day5CampfireDialoguePlayed = true;
                DialogueController.RequestDialogue(DialogueEventId.Day5CampfirePlaced);
            }

            RefreshDay5Stage();
            RefreshHud();
            return;
        }

        if (string.Equals(interactionKey, Day5CookedFoodInteractionKey, System.StringComparison.OrdinalIgnoreCase))
        {
            day5CookedFoodCount = Mathf.Min(Day5RequiredCookedFood, day5CookedFoodCount + amount);
            if (day5CookedFoodCount >= Day5RequiredCookedFood && !day5CookingCompleteDialoguePlayed)
            {
                day5CookingCompleteDialoguePending = true;
                TryRequestDay5CookingCompleteDialogue();
            }

            RefreshDay5Stage();
            RefreshHud();
            return;
        }

        if (string.Equals(interactionKey, Day5AteFoodInteractionKey, System.StringComparison.OrdinalIgnoreCase))
        {
            day5EatenFoodCount = Mathf.Min(Day5RequiredEatenFood, day5EatenFoodCount + amount);
            RefreshDay5Stage();
            RefreshHud();
            TryCompleteDay5SurvivalQuest();
        }
    }

    private void TryCompleteDay5SurvivalQuest()
    {
        if (!isQuestActive ||
            activeQuest == null ||
            activeQuest.ObjectiveType != QuestObjectiveType.Day5Survival ||
            isWaitingForCompletionDialogue)
        {
            return;
        }

        if (day5Stage == Day5SurvivalStage.Completed && currentProgress >= activeQuest.RequiredCount)
        {
            return;
        }

        SyncDay5InventoryProgress(false, false);
        if (!day5FishReady ||
            !day5WoodReady ||
            !day5CampfirePlaced ||
            day5CookedFoodCount < Day5RequiredCookedFood ||
            day5EatenFoodCount < Day5RequiredEatenFood)
        {
            RefreshDay5Stage();
            return;
        }

        day5Stage = Day5SurvivalStage.Completed;
        currentProgress = activeQuest.RequiredCount;
        BeginCompletionDialogue(activeQuest.CompletionDialogueEvent, true);
    }

    private void HandleFishingModeExited()
    {
        if (!IsDay5SurvivalQuestActive())
        {
            return;
        }

        SyncDay5InventoryProgress(true, false);
        RefreshDay5Stage();
        RefreshHud();
        TryCompleteDay5SurvivalQuest();
    }

    private bool IsDay5SurvivalQuestActive()
    {
        return isQuestActive &&
            activeQuest != null &&
            activeQuest.ObjectiveType == QuestObjectiveType.Day5Survival;
    }

    private bool IsDay5CookingObjectiveActive()
    {
        return IsDay5SurvivalQuestActive() &&
            day5CampfirePlaced &&
            day5CookedFoodCount < Day5RequiredCookedFood &&
            !isWaitingForCompletionDialogue &&
            !DialogueController.IsDialogueActive;
    }

    private bool IsDay5EatingObjectiveActive()
    {
        return IsDay5SurvivalQuestActive() &&
            day5CampfirePlaced &&
            day5CookedFoodCount >= Day5RequiredCookedFood &&
            day5EatenFoodCount < Day5RequiredEatenFood &&
            !isWaitingForCompletionDialogue &&
            !isWaitingForTurnIn &&
            !DialogueController.IsDialogueActive;
    }

    private void SyncDay5InventoryProgress(bool allowFishDialogue, bool allowWoodDialogue)
    {
        if (!IsDay5SurvivalQuestActive())
        {
            return;
        }

        ResolveReferences();
        int fishInInventory = CountInventoryItemsById(Day5FishItemId);
        int woodInInventory = CountInventoryItemsById(Day5WoodItemId);

        day5FishCount = Mathf.Min(Day5RequiredFish, Mathf.Max(day5FishCount, fishInInventory));
        day5WoodCount = Mathf.Min(Day5RequiredWood, Mathf.Max(day5WoodCount, woodInInventory));

        if (day5FishCount >= Day5RequiredFish)
        {
            day5FishReady = true;
            if (allowFishDialogue && !day5FishDialoguePlayed)
            {
                day5FishDialoguePlayed = true;
                DialogueController.RequestDialogue(DialogueEventId.Day5FishComplete);
            }
        }

        if (day5WoodCount >= Day5RequiredWood)
        {
            day5WoodReady = true;
            if (allowWoodDialogue && !day5WoodDialoguePlayed)
            {
                day5WoodDialoguePlayed = true;
                DialogueController.RequestDialogue(DialogueEventId.Day5WoodComplete);
            }
        }
    }

    private int CountInventoryItemsById(string itemId)
    {
        if (playerInventory == null || string.IsNullOrWhiteSpace(itemId))
        {
            return 0;
        }

        int totalAmount = 0;
        System.Collections.Generic.IReadOnlyList<PlayerInventory.InventorySlot> slots = playerInventory.Slots;
        for (int index = 0; index < slots.Count; index++)
        {
            PlayerInventory.InventorySlot slot = slots[index];
            if (slot != null && !slot.IsEmpty && MatchesItemId(slot.Item, itemId))
            {
                totalAmount += slot.Amount;
            }
        }

        return totalAmount;
    }

    private void RefreshDay5Stage()
    {
        if (!IsDay5SurvivalQuestActive())
        {
            day5Stage = Day5SurvivalStage.Inactive;
            return;
        }

        if (day5Stage == Day5SurvivalStage.Completed)
        {
            return;
        }

        if (!day5FishReady || !day5WoodReady)
        {
            day5Stage = Day5SurvivalStage.CatchFish;
            return;
        }

        if (!day5CampfirePlaced)
        {
            day5Stage = Day5SurvivalStage.PlaceCampfire;
            return;
        }

        if (day5CookedFoodCount < Day5RequiredCookedFood)
        {
            day5Stage = Day5SurvivalStage.CookFood;
            return;
        }

        if (day5EatenFoodCount < Day5RequiredEatenFood)
        {
            day5Stage = Day5SurvivalStage.EatFood;
            return;
        }

        day5Stage = Day5SurvivalStage.Completed;
    }

    private void TryRequestDay5CookingCompleteDialogue()
    {
        if (!IsDay5SurvivalQuestActive() ||
            !day5CookingCompleteDialoguePending ||
            day5CookingCompleteDialoguePlayed ||
            MiniGameCookingController.IsAnyMiniGameActive())
        {
            return;
        }

        day5CookingCompleteDialoguePending = false;
        day5CookingCompleteDialoguePlayed = true;
        RefreshDay5Stage();
        DialogueController.RequestDialogue(DialogueEventId.Day5CookingComplete);
    }

    private void HandleCookingMiniGameClosed()
    {
        if (!day5CookingCompleteDialoguePending || day5CookingCompleteDialoguePlayed)
        {
            return;
        }

        if (day5CookingCompleteDialogueRoutine != null)
        {
            StopCoroutine(day5CookingCompleteDialogueRoutine);
        }

        day5CookingCompleteDialogueRoutine = StartCoroutine(RequestDay5CookingCompleteDialogueAfterFrame());
    }

    private System.Collections.IEnumerator RequestDay5CookingCompleteDialogueAfterFrame()
    {
        yield return null;

        day5CookingCompleteDialogueRoutine = null;
        RestoreDay5EatingInteraction();
        TryRequestDay5CookingCompleteDialogue();
    }

    private void CancelDay5CookingCompleteDialogueRoutine()
    {
        if (day5CookingCompleteDialogueRoutine == null)
        {
            return;
        }

        StopCoroutine(day5CookingCompleteDialogueRoutine);
        day5CookingCompleteDialogueRoutine = null;
    }

    private void ScheduleDay5EatingInteractionRestore()
    {
        RestoreDay5EatingInteraction();

        if (day5EatingInteractionRestoreRoutine != null)
        {
            StopCoroutine(day5EatingInteractionRestoreRoutine);
        }

        day5EatingInteractionRestoreRoutine = StartCoroutine(RestoreDay5EatingInteractionAfterFrame());
    }

    private System.Collections.IEnumerator RestoreDay5EatingInteractionAfterFrame()
    {
        yield return null;

        day5EatingInteractionRestoreRoutine = null;
        RestoreDay5EatingInteraction();
        RefreshDay5Stage();
        RefreshHud();
    }

    private void CancelDay5EatingInteractionRestoreRoutine()
    {
        if (day5EatingInteractionRestoreRoutine == null)
        {
            return;
        }

        StopCoroutine(day5EatingInteractionRestoreRoutine);
        day5EatingInteractionRestoreRoutine = null;
    }

    private static void RestoreDay5EatingInteraction()
    {
        CampingCookingModeController[] controllers = FindObjectsByType<CampingCookingModeController>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (CampingCookingModeController controller in controllers)
        {
            if (controller != null && controller.IsCookingModeActive)
            {
                controller.ExitCookingMode();
            }
        }

        DailyQuestManager manager = TryGetInstance();
        if (manager == null || !manager.IsDay5EatingObjectiveActive())
        {
            return;
        }

        InventoryUIController inventory = FindActiveOrAnyBehaviour<InventoryUIController>();
        if (inventory != null && inventory.gameObject.activeInHierarchy && !inventory.enabled)
        {
            inventory.enabled = true;
        }

        PickUpScript pickUpScript = FindActiveOrAnyBehaviour<PickUpScript>();
        if (pickUpScript != null && pickUpScript.gameObject.activeInHierarchy && !pickUpScript.enabled)
        {
            pickUpScript.enabled = true;
        }

        PlayerUI playerUI = FindFirstObjectByType<PlayerUI>();
        if (playerUI != null)
        {
            playerUI.UpdatePrompt(string.Empty);
        }
    }

    private static T FindActiveOrAnyBehaviour<T>() where T : Behaviour
    {
        T[] behaviours = FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        T fallback = null;

        for (int index = 0; index < behaviours.Length; index++)
        {
            T behaviour = behaviours[index];
            if (behaviour == null)
            {
                continue;
            }

            fallback ??= behaviour;
            if (behaviour.gameObject.activeInHierarchy)
            {
                return behaviour;
            }
        }

        return fallback;
    }

    private static bool MatchesItemId(InventoryItemDefinition itemDefinition, string itemId)
    {
        return itemDefinition != null &&
            !string.IsNullOrWhiteSpace(itemId) &&
            string.Equals(itemDefinition.ItemId, itemId, System.StringComparison.OrdinalIgnoreCase);
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
        ApplyDay5DataPackageState(currentDay);
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

    private void ApplyDay5DataPackageState(DialogueDay currentDay)
    {
        if (currentDay != DialogueDay.Day5)
        {
            hasPreparedDay5DataPackageThisSession = false;
        }

        if ((int)currentDay < (int)DialogueDay.Day5 && (IsDay5DataCubeAppeared() || IsDay5DataCubeOpened()))
        {
            ClearDay5DataPackageState();
        }

        if (currentDay == DialogueDay.Day5 &&
            !hasPreparedDay5DataPackageThisSession &&
            !IsDay5SurvivalQuestActive() &&
            (IsDay5DataCubeAppeared() || IsDay5DataCubeOpened()))
        {
            ClearDay5DataPackageState();
        }

        bool isDay5 = currentDay == DialogueDay.Day5;
        bool isDay5OrLater = (int)currentDay >= (int)DialogueDay.Day5;
        bool cubeAppeared = isDay5 && IsDay5DataCubeAppeared();
        bool cubeOpened = isDay5OrLater && IsDay5DataCubeOpened();

        EnsureDay5DataCubeObject(cubeAppeared && !cubeOpened);
        EnsureDay5ComputerObject(cubeOpened);
    }

    private bool OpenDay5DataCube()
    {
        if (!CanOpenDay5DataCube())
        {
            return false;
        }

        SetDay5DataCubeOpened(true);
        ApplyDay5DataPackageState(DialogueDay.Day5);
        DialogueController.RequestDialogue(DialogueEventId.Day5ComputerOpened);
        return true;
    }

    private void EnsureDay5DataCubeObject(bool shouldShow)
    {
        GameObject dataCube = FindSceneObjectByName(Day5DataCubeObjectName, true);
        if (dataCube == null && shouldShow)
        {
            dataCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            dataCube.name = Day5DataCubeObjectName;
            dataCube.AddComponent<Day5DataCubeInteractable>();

            Renderer renderer = dataCube.GetComponent<Renderer>();
            if (renderer != null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    shader = Shader.Find("Standard");
                }

                if (shader != null)
                {
                    Material material = new Material(shader)
                    {
                        color = new Color(0.25f, 0.85f, 1f, 0.92f)
                    };
                    renderer.sharedMaterial = material;
                }
            }
        }

        ParentToDay5EventRoot(dataCube);

        if (dataCube == null)
        {
            return;
        }

        dataCube.transform.localScale = new Vector3(0.48f, 0.32f, 0.42f);
        PositionDay5ObjectOnTable(dataCube, dataCube.transform.lossyScale.y * 0.5f);
        dataCube.transform.rotation = ResolveDay5PackageRotation() * Quaternion.Euler(17f, 33f, -11f);
        dataCube.SetActive(shouldShow);
    }

    private void EnsureDay5ComputerObject(bool shouldShow)
    {
        GameObject computer = FindDay5ComputerObject();
        bool isAutoSpawnedComputer = false;
        if (computer == null && shouldShow)
        {
            computer = InstantiateDay5Computer();
            isAutoSpawnedComputer = true;
        }

        if (computer == null)
        {
            return;
        }

        ParentToDay5EventRoot(computer);

        if (isAutoSpawnedComputer)
        {
            PositionDay5ObjectOnTable(computer, 0.02f);
            FitObjectToDay5Table(computer, 1.25f);
        }

        computer.SetActive(shouldShow);
    }

    private static GameObject FindDay5ComputerObject()
    {
        for (int index = 0; index < Day5ComputerObjectNames.Length; index++)
        {
            GameObject computer = FindSceneObjectByName(Day5ComputerObjectNames[index], true);
            if (computer != null)
            {
                return computer;
            }
        }

        return null;
    }

    private GameObject InstantiateDay5Computer()
    {
        GameObject computerPrefab = null;

#if UNITY_EDITOR
        computerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Day5ComputerPrefabAssetPath);
        if (computerPrefab == null)
        {
            computerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Day5ComputerPrefabFallbackAssetPath);
        }
#endif

        GameObject computer = computerPrefab != null
            ? Instantiate(computerPrefab)
            : CreateFallbackDay5Computer();

        computer.name = Day5ComputerObjectName;
        ParentToDay5EventRoot(computer);
        return computer;
    }

    private static GameObject CreateFallbackDay5Computer()
    {
        GameObject root = new GameObject(Day5ComputerObjectName);
        GameObject monitor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        monitor.name = "Monitor";
        monitor.transform.SetParent(root.transform, false);
        monitor.transform.localPosition = new Vector3(0f, 0.45f, 0f);
        monitor.transform.localScale = new Vector3(0.95f, 0.58f, 0.08f);

        GameObject baseObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        baseObject.name = "Base";
        baseObject.transform.SetParent(root.transform, false);
        baseObject.transform.localPosition = new Vector3(0f, 0.08f, 0f);
        baseObject.transform.localScale = new Vector3(0.75f, 0.12f, 0.45f);

        return root;
    }

    private static void ParentToDay5EventRoot(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        Transform eventRoot = EnsureDay5EventRoot();
        if (eventRoot != null && target.transform.parent != eventRoot)
        {
            target.transform.SetParent(eventRoot, true);
        }
    }

    private static Transform EnsureDay5EventRoot()
    {
        GameObject eventRoot = FindSceneObjectByName(Day5EventRootObjectName, true);
        if (eventRoot == null)
        {
            eventRoot = new GameObject(Day5EventRootObjectName);
        }

        return eventRoot.transform;
    }

    private static void PositionDay5ObjectOnTable(GameObject target, float extraHeight)
    {
        if (target == null)
        {
            return;
        }

        Vector3 position = ResolveDay5PackagePosition(extraHeight);
        target.transform.SetPositionAndRotation(position, ResolveDay5PackageRotation());
    }

    private static Vector3 ResolveDay5PackagePosition(float extraHeight)
    {
        GameObject table = FindSceneObjectByName(Day5TableObjectName, true);
        if (table != null && TryGetCombinedBounds(table, out Bounds tableBounds))
        {
            return new Vector3(
                tableBounds.center.x,
                tableBounds.max.y + Mathf.Max(0f, extraHeight),
                tableBounds.center.z);
        }

        PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
        if (player != null)
        {
            Transform playerTransform = player.transform;
            return playerTransform.position + playerTransform.forward * 1.8f + Vector3.up * 1.1f;
        }

        return Vector3.up;
    }

    private static Quaternion ResolveDay5PackageRotation()
    {
        GameObject table = FindSceneObjectByName(Day5TableObjectName, true);
        if (table != null)
        {
            return Quaternion.Euler(0f, table.transform.eulerAngles.y, 0f);
        }

        PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
        if (player != null)
        {
            return Quaternion.Euler(0f, player.transform.eulerAngles.y, 0f);
        }

        return Quaternion.identity;
    }

    private static void FitObjectToDay5Table(GameObject target, float targetMaxSize)
    {
        if (target == null || !TryGetCombinedBounds(target, out Bounds bounds))
        {
            return;
        }

        float maxSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        if (maxSize > 0.0001f)
        {
            float scaleFactor = Mathf.Clamp(targetMaxSize / maxSize, 0.05f, 5f);
            target.transform.localScale *= scaleFactor;
        }

        if (!TryGetCombinedBounds(target, out bounds))
        {
            return;
        }

        Vector3 targetBottomCenter = ResolveDay5PackagePosition(0.02f);
        Vector3 currentBottomCenter = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        target.transform.position += targetBottomCenter - currentBottomCenter;
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

    private static bool IsDay5DataCubeAppeared()
    {
        return PlayerPrefs.GetInt(Day5DataCubeAppearedKey, 0) == 1;
    }

    private static void SetDay5DataCubeAppeared(bool hasAppeared)
    {
        if (hasAppeared)
        {
            PlayerPrefs.SetInt(Day5DataCubeAppearedKey, 1);
        }
        else
        {
            PlayerPrefs.DeleteKey(Day5DataCubeAppearedKey);
        }

        PlayerPrefs.Save();
    }

    private static void ClearDay5DataPackageState()
    {
        PlayerPrefs.DeleteKey(Day5DataCubeAppearedKey);
        PlayerPrefs.DeleteKey(Day5DataCubeOpenedKey);
        PlayerPrefs.Save();
    }

    private static bool IsDay5DataCubeOpened()
    {
        return PlayerPrefs.GetInt(Day5DataCubeOpenedKey, 0) == 1;
    }

    private static void SetDay5DataCubeOpened(bool isOpened)
    {
        if (isOpened)
        {
            PlayerPrefs.SetInt(Day5DataCubeOpenedKey, 1);
        }
        else
        {
            PlayerPrefs.DeleteKey(Day5DataCubeOpenedKey);
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

    private static bool TryGetCombinedBounds(GameObject target, out Bounds bounds)
    {
        bounds = default;
        if (target == null)
        {
            return false;
        }

        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        if (renderers != null && renderers.Length > 0)
        {
            bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
            {
                Renderer renderer = renderers[index];
                if (renderer != null)
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return true;
        }

        Collider[] colliders = target.GetComponentsInChildren<Collider>(true);
        if (colliders == null || colliders.Length == 0)
        {
            return false;
        }

        bounds = colliders[0].bounds;
        for (int index = 1; index < colliders.Length; index++)
        {
            Collider collider = colliders[index];
            if (collider != null)
            {
                bounds.Encapsulate(collider.bounds);
            }
        }

        return true;
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
        ResetDay5SurvivalState();
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

        if (activeQuest.ObjectiveType == QuestObjectiveType.Day5Survival)
        {
            SyncDay5InventoryProgress(false, false);
            RefreshDay5Stage();
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

        if (activeQuest.ObjectiveType == QuestObjectiveType.Day5Survival)
        {
            return string.Concat(
                $"Cá: {day5FishCount}/{Day5RequiredFish}\n",
                $"Gỗ: {day5WoodCount}/{Day5RequiredWood}\n",
                $"Món ăn: {day5CookedFoodCount}/{Day5RequiredCookedFood}\n",
                $"Ăn: {day5EatenFoodCount}/{Day5RequiredEatenFood}");
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

        if (activeQuest.ObjectiveType == QuestObjectiveType.Day5Survival)
        {
            return day5Stage switch
            {
                Day5SurvivalStage.CatchFish => "Hướng dẫn: Chuẩn bị đủ 7 con cá và 5 khúc gỗ trong túi đồ.",
                Day5SurvivalStage.GatherWood => "Hướng dẫn: Chuẩn bị đủ 7 con cá và 5 khúc gỗ trong túi đồ.",
                Day5SurvivalStage.PlaceCampfire => "Hướng dẫn: Dùng gỗ trong túi đồ để đặt campfire.",
                Day5SurvivalStage.CookFood => "Hướng dẫn: Nấu đủ 3 món ăn bằng campfire.",
                Day5SurvivalStage.EatFood => "Hướng dẫn: Ăn 1 món đã nấu.",
                _ => string.Empty
            };
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
        if (activeQuest != null &&
            activeQuest.ObjectiveType == QuestObjectiveType.Day5Survival &&
            day5Stage == Day5SurvivalStage.Completed)
        {
            return "Hoàn thành nhiệm vụ.";
        }

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
