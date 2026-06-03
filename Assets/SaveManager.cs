using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; set; }
    public static bool IsLoadingSavedGame { get; private set; }
    public static bool ShouldSuppressAutoStartStory { get; private set; }
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }

        DontDestroyOnLoad(gameObject);
    }

    string jsonPathProject;

    string jsonPathPersistant;
    string binaryPath;

    string fileName = "SaveGame";

    private static readonly string[] StoryFlagKeys =
    {
        "quest.gatherWood.bundlePlaced",
        "quest.day3.carpetsShown",
        "quest.day5.dataCubeAppeared",
        "quest.day5.dataCubeOpened",
        "quest.day6.pcOpened",
        "quest.day6.toiletPulled",
        "day3Hint.unlocked",
        "day3Hint.completed"
    };

    private static readonly string[] StoryIntKeys =
    {
        "day3Hint.lastKnownDay",
        "simplePickup.campaignId",
        "simplePickup.lastKnownDay"
    };


    public bool isSavingJson;

    private void Start()
    {
        jsonPathProject = Application.dataPath + Path.AltDirectorySeparatorChar;
        jsonPathPersistant = Application.persistentDataPath + Path.AltDirectorySeparatorChar;
        binaryPath = Application.persistentDataPath + Path.AltDirectorySeparatorChar;
    }


    public AllGameData LoadingTypeSwitch(int slotNumber)
    {
        if(isSavingJson)
        {
            AllGameData gameData = LoadGameDataFromJsonFile(slotNumber);
            return gameData;
        }
        else
        {
            AllGameData gameData = LoadGameDataFromBinaryFile(slotNumber);
            return gameData;
        }
    }

    public void LoadGame(int slotNumber)
    {
        AllGameData gameData = LoadingTypeSwitch(slotNumber);
        if (gameData == null)
        {
            IsLoadingSavedGame = false;
            return;
        }

        SetStoryData(gameData.storyData);
        SetPlayerData(gameData.playerData);
        SetInventoryData(gameData.inventoryData);
        RestoreDialogueProgress(gameData.storyData != null ? gameData.storyData.dialogueProgress : null);
        RestoreGameplayStateAfterLoad(gameData.storyData != null ? gameData.storyData.dialogueProgress : null);
        StartCoroutine(ClearLoadedGameFlagsAfterFrame());

    }

    private void SetPlayerData(PlayerData playerData)
    {
        if (playerData == null)
        {
            return;
        }

        if (playerData.playerStats != null && playerData.playerStats.Length >= 3)
        {
            PlayerState.Instance.currentHealthy = playerData.playerStats[0];
            PlayerState.Instance.currentCarlories = playerData.playerStats[1];
            PlayerState.Instance.currentHydrationPercent = playerData.playerStats[2];
        }
        
        if (playerData.playerPositionAndRotation == null || playerData.playerPositionAndRotation.Length < 6)
        {
            SetStaminaData(playerData.staminaData);
            return;
        }

        Vector3 loadedPosition;
        loadedPosition.x = playerData.playerPositionAndRotation[0];
        loadedPosition.y = playerData.playerPositionAndRotation[1];
        loadedPosition.z = playerData.playerPositionAndRotation[2];

        PlayerState.Instance.playerBody.transform.position = loadedPosition;


        Vector3 loadedRotation;
        loadedRotation.x = playerData.playerPositionAndRotation[3];
        loadedRotation.y = playerData.playerPositionAndRotation[4];
        loadedRotation.z = playerData.playerPositionAndRotation[5];

        PlayerState.Instance.playerBody.transform.rotation = Quaternion.Euler(loadedRotation);

        SetStaminaData(playerData.staminaData);
    }


    public void StartLoadedGame(int slotNumber)
    {
        IsLoadingSavedGame = true;
        ShouldSuppressAutoStartStory = true;

        AllGameData gameData = LoadingTypeSwitch(slotNumber);
        if (gameData != null)
        {
            SetStoryData(gameData.storyData, false);
        }

        if (!LoadingManager.LoadScene("InGame"))
        {
            SceneManager.LoadScene("InGame");
        }

        StartCoroutine(DelayedLoading(slotNumber));
    }
    private IEnumerator DelayedLoading(int slotNumber)
    {
        while (SceneManager.GetActiveScene().name != "InGame")
        {
            yield return null;
        }

        yield return null;
        LoadGame(slotNumber);

        print("Game Loaded");
    }

    private IEnumerator ClearLoadedGameFlagsAfterFrame()
    {
        yield return null;
        IsLoadingSavedGame = false;
        ShouldSuppressAutoStartStory = false;
    }


    public void SavingTypeSwitch(AllGameData gameData, int slotNumber)
    {
        if(isSavingJson)
        {
            SaveGameDataToJsonFile(gameData, slotNumber);
        }
        else 
        {
            SaveGameDataToBinaryFile(gameData, slotNumber);
        }
    }

    public void SaveGame(int slotNumber)
    {
        AllGameData data = new AllGameData();

        data.playerData = GetPlayerData();
        data.inventoryData = GetInventoryData();
        data.storyData = GetStoryData();
        SavingTypeSwitch(data, slotNumber);
    }

    private PlayerData GetPlayerData()
    {
        float[] playerStats = new float[3];
        playerStats[0] = PlayerState.Instance.currentHealthy;
        playerStats[1] = PlayerState.Instance.currentCarlories;
        playerStats[2] = PlayerState.Instance.currentHydrationPercent;

        float[] playerPosAndRot = new float[6];
        playerPosAndRot[0] = PlayerState.Instance.playerBody.transform.position.x;
        playerPosAndRot[1] = PlayerState.Instance.playerBody.transform.position.y;
        playerPosAndRot[2] = PlayerState.Instance.playerBody.transform.position.z;

        playerPosAndRot[3] = PlayerState.Instance.playerBody.transform.rotation.x;
        playerPosAndRot[4] = PlayerState.Instance.playerBody.transform.rotation.y;
        playerPosAndRot[5] = PlayerState.Instance.playerBody.transform.rotation.z;

        float[] staminaData = GetStaminaData();

        return new PlayerData(playerStats, playerPosAndRot, staminaData);
    }

    private float[] GetStaminaData()
    {
        Stamina stamina = FindFirstObjectByType<Stamina>();
        if (stamina == null || stamina.staminaSlider == null)
        {
            return null;
        }

        return new float[]
        {
            stamina.staminaSlider.value,
            stamina.staminaSlider.maxValue,
            stamina.maxStamina
        };
    }

    private void SetStaminaData(float[] staminaData)
    {
        if (staminaData == null || staminaData.Length < 1)
        {
            return;
        }

        Stamina stamina = FindFirstObjectByType<Stamina>();
        if (stamina == null || stamina.staminaSlider == null)
        {
            return;
        }

        if (staminaData.Length >= 3 && staminaData[2] > 0f)
        {
            stamina.maxStamina = staminaData[2];
        }

        float maxValue = staminaData.Length >= 2 && staminaData[1] > 0f
            ? staminaData[1]
            : stamina.maxStamina;

        stamina.staminaSlider.maxValue = maxValue;
        stamina.staminaSlider.value = Mathf.Clamp(staminaData[0], 0f, maxValue);
    }

    private InventorySaveData GetInventoryData()
    {
        PlayerInventory inventory = FindFirstObjectByType<PlayerInventory>();
        if (inventory == null)
        {
            return null;
        }

        InventorySaveData inventoryData = new InventorySaveData();
        inventoryData.slots = new InventorySlotSaveData[inventory.SlotCount];

        for (int i = 0; i < inventory.SlotCount; i++)
        {
            PlayerInventory.InventorySlot slot = inventory.Slots[i];
            InventorySlotSaveData slotData = new InventorySlotSaveData();

            if (slot != null && !slot.IsEmpty && slot.Item != null)
            {
                slotData.itemId = slot.Item.ItemId;
                slotData.amount = slot.Amount;
            }

            inventoryData.slots[i] = slotData;
        }

        return inventoryData;
    }

    private void SetInventoryData(InventorySaveData inventoryData)
    {
        PlayerInventory inventory = FindFirstObjectByType<PlayerInventory>();
        if (inventory == null || inventoryData == null || inventoryData.slots == null)
        {
            return;
        }

        Dictionary<string, InventoryItemDefinition> itemDefinitions = BuildInventoryItemLookup();
        int slotCount = Mathf.Min(inventory.SlotCount, inventoryData.slots.Length);

        for (int i = 0; i < inventory.SlotCount; i++)
        {
            inventory.Slots[i].Clear();
        }

        for (int i = 0; i < slotCount; i++)
        {
            InventorySlotSaveData slotData = inventoryData.slots[i];
            if (slotData == null || string.IsNullOrWhiteSpace(slotData.itemId) || slotData.amount <= 0)
            {
                continue;
            }

            if (itemDefinitions.TryGetValue(slotData.itemId, out InventoryItemDefinition itemDefinition))
            {
                inventory.Slots[i].Set(itemDefinition, slotData.amount);
            }
            else
            {
                Debug.LogWarning($"SaveManager could not restore inventory item '{slotData.itemId}' because no matching InventoryItemDefinition was found in Resources.");
            }
        }
    }

    private Dictionary<string, InventoryItemDefinition> BuildInventoryItemLookup()
    {
        Dictionary<string, InventoryItemDefinition> itemDefinitions = new Dictionary<string, InventoryItemDefinition>(StringComparer.OrdinalIgnoreCase);
        InventoryItemDefinition[] loadedDefinitions = Resources.LoadAll<InventoryItemDefinition>(string.Empty);

        for (int i = 0; i < loadedDefinitions.Length; i++)
        {
            InventoryItemDefinition definition = loadedDefinitions[i];
            if (definition == null)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(definition.ItemId) && !itemDefinitions.ContainsKey(definition.ItemId))
            {
                itemDefinitions.Add(definition.ItemId, definition);
            }

            if (!string.IsNullOrWhiteSpace(definition.name) && !itemDefinitions.ContainsKey(definition.name))
            {
                itemDefinitions.Add(definition.name, definition);
            }
        }

        return itemDefinitions;
    }

    private StorySaveData GetStoryData()
    {
        StorySaveData storyData = new StorySaveData();
        storyData.currentDay = (int)DialogueSaveService.GetCurrentDay();
        storyData.flags = new StoryFlagSaveData[StoryFlagKeys.Length];
        storyData.intValues = new StoryIntSaveData[StoryIntKeys.Length];
        storyData.questProgress = GetQuestProgressData();
        storyData.dialogueProgress = GetDialogueProgressData();

        for (int i = 0; i < StoryFlagKeys.Length; i++)
        {
            string key = StoryFlagKeys[i];
            storyData.flags[i] = new StoryFlagSaveData()
            {
                key = key,
                value = PlayerPrefs.GetInt(key, 0) == 1
            };
        }

        for (int i = 0; i < StoryIntKeys.Length; i++)
        {
            string key = StoryIntKeys[i];
            storyData.intValues[i] = new StoryIntSaveData()
            {
                key = key,
                value = PlayerPrefs.GetInt(key, 0)
            };
        }

        return storyData;
    }

    private void SetStoryData(StorySaveData storyData, bool restoreQuestProgress = true)
    {
        if (storyData == null)
        {
            return;
        }

        if (storyData.currentDay > 0)
        {
            DialogueSaveService.SetCurrentDay((DialogueDay)storyData.currentDay);
        }

        if (storyData.flags != null)
        {
            for (int i = 0; i < storyData.flags.Length; i++)
            {
                StoryFlagSaveData flag = storyData.flags[i];
                if (flag == null || string.IsNullOrWhiteSpace(flag.key))
                {
                    continue;
                }

                if (flag.value)
                {
                    PlayerPrefs.SetInt(flag.key, 1);
                }
                else
                {
                    PlayerPrefs.DeleteKey(flag.key);
                }
            }
        }

        if (storyData.intValues != null)
        {
            for (int i = 0; i < storyData.intValues.Length; i++)
            {
                StoryIntSaveData intValue = storyData.intValues[i];
                if (intValue == null || string.IsNullOrWhiteSpace(intValue.key))
                {
                    continue;
                }

                PlayerPrefs.SetInt(intValue.key, intValue.value);
            }
        }

        PlayerPrefs.Save();

        if (restoreQuestProgress)
        {
            RestoreQuestProgress(storyData.questProgress);
        }
    }

    private QuestProgressSaveData GetQuestProgressData()
    {
        DailyQuestManager questManager = FindFirstObjectByType<DailyQuestManager>();
        if (questManager == null)
        {
            return null;
        }

        QuestProgressSaveData questProgress = new QuestProgressSaveData();
        questProgress.isQuestActive = GetPrivateField<bool>(questManager, "isQuestActive");
        questProgress.currentProgress = GetPrivateField<int>(questManager, "currentProgress");
        questProgress.isWaitingForCompletionDialogue = GetPrivateField<bool>(questManager, "isWaitingForCompletionDialogue");
        questProgress.isWaitingForTurnIn = GetPrivateField<bool>(questManager, "isWaitingForTurnIn");
        questProgress.shouldAdvanceAfterPendingDialogue = GetPrivateField<bool>(questManager, "shouldAdvanceAfterPendingDialogue");
        questProgress.pendingCompletionDialogueEvent = (int)GetPrivateField<DialogueEventId>(questManager, "pendingCompletionDialogueEvent");
        questProgress.day5Stage = GetPrivateEnumInt(questManager, "day5Stage");
        questProgress.day5FishCount = GetPrivateField<int>(questManager, "day5FishCount");
        questProgress.day5WoodCount = GetPrivateField<int>(questManager, "day5WoodCount");
        questProgress.day5CookedFoodCount = GetPrivateField<int>(questManager, "day5CookedFoodCount");
        questProgress.day5EatenFoodCount = GetPrivateField<int>(questManager, "day5EatenFoodCount");
        questProgress.day5FishReady = GetPrivateField<bool>(questManager, "day5FishReady");
        questProgress.day5WoodReady = GetPrivateField<bool>(questManager, "day5WoodReady");
        questProgress.day5FishDialoguePlayed = GetPrivateField<bool>(questManager, "day5FishDialoguePlayed");
        questProgress.day5WoodDialoguePlayed = GetPrivateField<bool>(questManager, "day5WoodDialoguePlayed");
        questProgress.day5CampfirePlaced = GetPrivateField<bool>(questManager, "day5CampfirePlaced");
        questProgress.day5CampfireDialoguePlayed = GetPrivateField<bool>(questManager, "day5CampfireDialoguePlayed");
        questProgress.day5CookingCompleteDialoguePending = GetPrivateField<bool>(questManager, "day5CookingCompleteDialoguePending");
        questProgress.day5CookingCompleteDialoguePlayed = GetPrivateField<bool>(questManager, "day5CookingCompleteDialoguePlayed");
        questProgress.day5GunshotFollowupRequested = GetPrivateField<bool>(questManager, "day5GunshotFollowupRequested");
        questProgress.day6Stage = GetPrivateEnumInt(questManager, "day6Stage");

        DailyQuestDefinition activeQuest = GetPrivateField<DailyQuestDefinition>(questManager, "activeQuest");
        if (activeQuest != null)
        {
            questProgress.questDay = (int)activeQuest.Day;
            questProgress.questId = (int)activeQuest.QuestId;
        }

        return questProgress;
    }

    private void RestoreQuestProgress(QuestProgressSaveData questProgress)
    {
        DailyQuestManager questManager = FindFirstObjectByType<DailyQuestManager>();
        if (questManager == null || questProgress == null)
        {
            return;
        }

        if (!questProgress.isQuestActive || questProgress.questId == (int)DailyQuestId.None || questProgress.questDay <= 0)
        {
            SetPrivateField(questManager, "activeQuest", null);
            SetPrivateField(questManager, "currentProgress", 0);
            SetPrivateField(questManager, "isQuestActive", false);
            InvokePrivateMethod(questManager, "RefreshHud");
            return;
        }

        MethodInfo startQuestMethod = GetPrivateMethod(questManager, "StartQuest");
        if (startQuestMethod != null)
        {
            startQuestMethod.Invoke(questManager, new object[]
            {
                (DialogueDay)questProgress.questDay,
                (DailyQuestId)questProgress.questId
            });
        }

        SetPrivateField(questManager, "currentProgress", questProgress.currentProgress);
        SetPrivateField(questManager, "isQuestActive", questProgress.isQuestActive);
        SetPrivateField(questManager, "isWaitingForCompletionDialogue", questProgress.isWaitingForCompletionDialogue);
        SetPrivateField(questManager, "isWaitingForTurnIn", questProgress.isWaitingForTurnIn);
        SetPrivateField(questManager, "pendingCompletionDialogueEvent", (DialogueEventId)questProgress.pendingCompletionDialogueEvent);
        SetPrivateField(questManager, "shouldAdvanceAfterPendingDialogue", questProgress.shouldAdvanceAfterPendingDialogue);
        SetPrivateEnumInt(questManager, "day5Stage", questProgress.day5Stage);
        SetPrivateField(questManager, "day5FishCount", questProgress.day5FishCount);
        SetPrivateField(questManager, "day5WoodCount", questProgress.day5WoodCount);
        SetPrivateField(questManager, "day5CookedFoodCount", questProgress.day5CookedFoodCount);
        SetPrivateField(questManager, "day5EatenFoodCount", questProgress.day5EatenFoodCount);
        SetPrivateField(questManager, "day5FishReady", questProgress.day5FishReady);
        SetPrivateField(questManager, "day5WoodReady", questProgress.day5WoodReady);
        SetPrivateField(questManager, "day5FishDialoguePlayed", questProgress.day5FishDialoguePlayed);
        SetPrivateField(questManager, "day5WoodDialoguePlayed", questProgress.day5WoodDialoguePlayed);
        SetPrivateField(questManager, "day5CampfirePlaced", questProgress.day5CampfirePlaced);
        SetPrivateField(questManager, "day5CampfireDialoguePlayed", questProgress.day5CampfireDialoguePlayed);
        SetPrivateField(questManager, "day5CookingCompleteDialoguePending", questProgress.day5CookingCompleteDialoguePending);
        SetPrivateField(questManager, "day5CookingCompleteDialoguePlayed", questProgress.day5CookingCompleteDialoguePlayed);
        SetPrivateField(questManager, "day5GunshotFollowupRequested", questProgress.day5GunshotFollowupRequested);
        SetPrivateEnumInt(questManager, "day6Stage", questProgress.day6Stage);

        InvokePrivateMethod(questManager, "ApplyPersistentSceneState");
        InvokePrivateMethod(questManager, "RefreshHud");
    }

    private DialogueProgressSaveData GetDialogueProgressData()
    {
        DialogueController dialogueController = FindFirstObjectByType<DialogueController>();
        if (dialogueController == null)
        {
            return null;
        }

        DialogueProgressSaveData dialogueProgress = new DialogueProgressSaveData();
        dialogueProgress.isDialogueActive = GetPrivateField<bool>(dialogueController, "isDialogueActive");
        dialogueProgress.currentLineIndex = GetPrivateField<int>(dialogueController, "currentLineIndex");

        object currentRequest = GetPrivateField<object>(dialogueController, "currentRequest");
        if (currentRequest != null)
        {
            dialogueProgress.currentDay = (int)GetPublicProperty<DialogueDay>(currentRequest, "Day");
            dialogueProgress.currentEventId = (int)GetPublicProperty<DialogueEventId>(currentRequest, "EventId");
        }

        return dialogueProgress;
    }

    private void RestoreDialogueProgress(DialogueProgressSaveData dialogueProgress)
    {
        if (dialogueProgress == null ||
            !dialogueProgress.isDialogueActive ||
            dialogueProgress.currentEventId == (int)DialogueEventId.None ||
            IsAutoStartDialogue((DialogueEventId)dialogueProgress.currentEventId))
        {
            return;
        }

        DialogueController.SetCurrentDay((DialogueDay)dialogueProgress.currentDay);
        DialogueController.RequestDialogue((DialogueEventId)dialogueProgress.currentEventId);

        DialogueController dialogueController = FindFirstObjectByType<DialogueController>();
        MethodInfo showLineMethod = GetPrivateMethod(dialogueController, "ShowLine");
        if (showLineMethod != null && dialogueProgress.currentLineIndex > 0)
        {
            showLineMethod.Invoke(dialogueController, new object[] { dialogueProgress.currentLineIndex });
        }
    }

    private void RestoreGameplayStateAfterLoad(DialogueProgressSaveData dialogueProgress)
    {
        Time.timeScale = 1f;
        Input.ResetInputAxes();

        bool shouldKeepDialogueControlsLocked = dialogueProgress != null &&
            dialogueProgress.isDialogueActive &&
            dialogueProgress.currentEventId != (int)DialogueEventId.None;

        MenuManager menuManager = FindFirstObjectByType<MenuManager>(FindObjectsInactive.Include);
        if (menuManager != null)
        {
            menuManager.isMenuOpen = false;
            SetGameObjectActive(menuManager.menuCanvas, false);
            SetGameObjectActive(menuManager.saveMenu, false);
            SetGameObjectActive(menuManager.loadMenu, false);
            SetGameObjectActive(menuManager.settingsMenu, false);
            SetGameObjectActive(menuManager.subMenu, true);
            SetGameObjectActive(menuManager.uiCanvas, true);
        }

        InventoryUIController inventoryUIController = FindFirstObjectByType<InventoryUIController>(FindObjectsInactive.Include);
        bool isInventoryOpen = inventoryUIController != null && inventoryUIController.IsInventoryOpen;

        if (!shouldKeepDialogueControlsLocked && !isInventoryOpen)
        {
            EnableGameplayControls();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        EnsurePlayerCameraActive();
    }

    private void EnableGameplayControls()
    {
        EnableBehaviour(FindFirstObjectByType<PlayerMovement>(FindObjectsInactive.Include));
        EnableBehaviour(FindFirstObjectByType<Jump>(FindObjectsInactive.Include));
        EnableBehaviour(FindFirstObjectByType<Crouch>(FindObjectsInactive.Include));
        EnableBehaviour(FindFirstObjectByType<ActionScript>(FindObjectsInactive.Include));
        EnableBehaviour(FindFirstObjectByType<Zoom>(FindObjectsInactive.Include));
        EnableBehaviour(FindFirstObjectByType<PickUpScript>(FindObjectsInactive.Include));
        EnableBehaviour(FindFirstObjectByType<CuttingTreeSystem>(FindObjectsInactive.Include));

        PlayerLook[] playerLooks = FindObjectsByType<PlayerLook>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < playerLooks.Length; i++)
        {
            EnableBehaviour(playerLooks[i]);
        }

        MouseMovement[] mouseMovements = FindObjectsByType<MouseMovement>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < mouseMovements.Length; i++)
        {
            EnableBehaviour(mouseMovements[i]);
        }
    }

    private void EnsurePlayerCameraActive()
    {
        Camera mainCamera = null;
        PlayerMovement playerMovement = FindFirstObjectByType<PlayerMovement>(FindObjectsInactive.Include);
        if (playerMovement != null)
        {
            mainCamera = GetPrivateField<Camera>(playerMovement, "cameraMain");
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera == null)
        {
            return;
        }

        Camera[] cameras = FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < cameras.Length; i++)
        {
            Camera camera = cameras[i];
            if (camera == null)
            {
                continue;
            }

            bool shouldEnable = camera == mainCamera;
            camera.enabled = shouldEnable;

            AudioListener audioListener = camera.GetComponent<AudioListener>();
            if (audioListener != null)
            {
                audioListener.enabled = shouldEnable;
            }
        }
    }

    private static void EnableBehaviour(Behaviour behaviour)
    {
        if (behaviour != null)
        {
            behaviour.enabled = true;
        }
    }

    private static void SetGameObjectActive(GameObject target, bool isActive)
    {
        if (target != null)
        {
            target.SetActive(isActive);
        }
    }

    private static bool IsAutoStartDialogue(DialogueEventId eventId)
    {
        return eventId == DialogueEventId.IntroWakeUp ||
            eventId == DialogueEventId.DayStart;
    }

    private static FieldInfo GetPrivateFieldInfo(object target, string fieldName)
    {
        return target != null
            ? target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            : null;
    }

    private static MethodInfo GetPrivateMethod(object target, string methodName)
    {
        return target != null
            ? target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
            : null;
    }

    private static T GetPrivateField<T>(object target, string fieldName)
    {
        FieldInfo fieldInfo = GetPrivateFieldInfo(target, fieldName);
        if (fieldInfo == null)
        {
            return default;
        }

        object value = fieldInfo.GetValue(target);
        return value is T typedValue ? typedValue : default;
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo fieldInfo = GetPrivateFieldInfo(target, fieldName);
        if (fieldInfo != null)
        {
            fieldInfo.SetValue(target, value);
        }
    }

    private static int GetPrivateEnumInt(object target, string fieldName)
    {
        FieldInfo fieldInfo = GetPrivateFieldInfo(target, fieldName);
        object value = fieldInfo != null ? fieldInfo.GetValue(target) : null;
        return value != null ? Convert.ToInt32(value) : 0;
    }

    private static void SetPrivateEnumInt(object target, string fieldName, int value)
    {
        FieldInfo fieldInfo = GetPrivateFieldInfo(target, fieldName);
        if (fieldInfo != null && fieldInfo.FieldType.IsEnum)
        {
            fieldInfo.SetValue(target, Enum.ToObject(fieldInfo.FieldType, value));
        }
    }

    private static void InvokePrivateMethod(object target, string methodName)
    {
        MethodInfo methodInfo = GetPrivateMethod(target, methodName);
        if (methodInfo != null)
        {
            methodInfo.Invoke(target, null);
        }
    }

    private static T GetPublicProperty<T>(object target, string propertyName)
    {
        PropertyInfo propertyInfo = target != null
            ? target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)
            : null;
        if (propertyInfo == null)
        {
            return default;
        }

        object value = propertyInfo.GetValue(target);
        return value is T typedValue ? typedValue : default;
    }


#region To Binary Section

    public void SaveGameDataToBinaryFile(AllGameData gameData, int slotNumber)
    {
        BinaryFormatter formatter = new BinaryFormatter();

        FileStream steam = new FileStream(binaryPath + fileName + slotNumber + ".binary", FileMode.Create);

        formatter.Serialize(steam, gameData);
        steam.Close();

        print ("Data saved to" + binaryPath + fileName + slotNumber + ".binary");
       
    }

    public AllGameData LoadGameDataFromBinaryFile(int slotNumber)
    {
        if (File.Exists(binaryPath + fileName + slotNumber + ".binary"))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream steam = new FileStream(binaryPath + fileName + slotNumber + ".binary", FileMode.Open);

            AllGameData gameData = formatter.Deserialize(steam) as AllGameData;
            steam.Close();

            print ("Data loaded from" + binaryPath + fileName + slotNumber + ".binary");

            return gameData;
        }
        else
        {
            return null;
        }
    }

#endregion


    public void SaveGameDataToJsonFile(AllGameData gameData , int slotNumber)
    {
       String json = JsonUtility.ToJson(gameData);

       String encrypted = EncryptionDecryption(json);

       using (StreamWriter writer = new StreamWriter(jsonPathProject + fileName + slotNumber + ".json"))
       {
           writer.Write(encrypted);
           print ("Saved Game to Json file at:" + jsonPathProject + fileName + slotNumber + ".json");
       };

    }

    public AllGameData LoadGameDataFromJsonFile(int slotNumber)
    {
        using (StreamReader reader = new StreamReader(jsonPathProject + fileName + slotNumber + ".json"))
        {
            string json = reader.ReadToEnd();

            string decrypted = EncryptionDecryption(json);

            AllGameData gameData = JsonUtility.FromJson<AllGameData>(decrypted);
            return gameData;
        };


    }


    [System.Serializable]
    public class VolumeSettings
    {
        public float music;
        public float sound;
        public float master;
    }

    public void SaveVolumeSettings(float _music, float _sound, float _master)
    {
        VolumeSettings volumeSettings = new VolumeSettings()
        {
            music = _music,
            sound = _sound,
            master = _master
        };    

        PlayerPrefs.SetString("Volume", JsonUtility.ToJson(volumeSettings));
        PlayerPrefs.Save();

        print("Saved to Player Pref");
    }

    public VolumeSettings LoadVolumeSettings()
    {
        return JsonUtility.FromJson<VolumeSettings>(PlayerPrefs.GetString("Volume"));
    }

#region Encryption

    public string EncryptionDecryption(string jsonString)
    {
        string keyword = "1234567";
        string result = "";
        for (int i = 0; i < jsonString.Length; i++)
        {
            result += (char)(jsonString[i] ^ keyword[i % keyword.Length]);
        }
        return result;
    }

#endregion


#region Utility
public bool DoesFileExists(int slotNumber)
    {
        if (isSavingJson)
        {
            if (System.IO.File.Exists(jsonPathProject + fileName + slotNumber + ".json"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        else
        {
            if (System.IO.File.Exists(binaryPath + fileName + slotNumber + ".binary") ||
                System.IO.File.Exists(binaryPath + fileName + slotNumber + ".bin"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public bool IsSlotEmpty(int slotNumber)
    {
        if (DoesFileExists(slotNumber))
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    public void DeselectButton()
    {
        GameObject myEventSystem = GameObject.Find("EventSystem");
        myEventSystem.GetComponent<UnityEngine.EventSystems.EventSystem>().SetSelectedGameObject(null);
    }

#endregion



}
