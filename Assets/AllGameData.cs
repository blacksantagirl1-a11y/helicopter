using UnityEngine;

[System.Serializable]
public class AllGameData
{
   public PlayerData playerData;
   public InventorySaveData inventoryData;
   public StorySaveData storyData;

}

[System.Serializable]
public class InventorySaveData
{
   public InventorySlotSaveData[] slots;
}

[System.Serializable]
public class InventorySlotSaveData
{
   public string itemId;
   public int amount;
}

[System.Serializable]
public class StorySaveData
{
   public int currentDay;
   public StoryFlagSaveData[] flags;
   public StoryIntSaveData[] intValues;
   public QuestProgressSaveData questProgress;
   public DialogueProgressSaveData dialogueProgress;
}

[System.Serializable]
public class StoryFlagSaveData
{
   public string key;
   public bool value;
}

[System.Serializable]
public class StoryIntSaveData
{
   public string key;
   public int value;
}

[System.Serializable]
public class QuestProgressSaveData
{
   public bool isQuestActive;
   public int questDay;
   public int questId;
   public int currentProgress;
   public bool isWaitingForCompletionDialogue;
   public bool isWaitingForTurnIn;
   public int pendingCompletionDialogueEvent;
   public bool shouldAdvanceAfterPendingDialogue;
   public int day5Stage;
   public int day5FishCount;
   public int day5WoodCount;
   public int day5CookedFoodCount;
   public int day5EatenFoodCount;
   public bool day5FishReady;
   public bool day5WoodReady;
   public bool day5FishDialoguePlayed;
   public bool day5WoodDialoguePlayed;
   public bool day5CampfirePlaced;
   public bool day5CampfireDialoguePlayed;
   public bool day5CookingCompleteDialoguePending;
   public bool day5CookingCompleteDialoguePlayed;
   public bool day5GunshotFollowupRequested;
   public int day6Stage;
}

[System.Serializable]
public class DialogueProgressSaveData
{
   public bool isDialogueActive;
   public int currentDay;
   public int currentEventId;
   public int currentLineIndex;
}
