using UnityEngine;

[System.Serializable]
public class AllGameData
{
   // Goi tong tat ca du lieu can luu cho mot slot save.
   public PlayerData playerData;
   public InventorySaveData inventoryData;
   public StorySaveData storyData;

}

[System.Serializable]
public class InventorySaveData
{
   // Mang slot giu thong tin item va so luong trong inventory.
   public InventorySlotSaveData[] slots;
}

[System.Serializable]
public class InventorySlotSaveData
{
   // itemId lien ket lai voi InventoryItemDefinition khi load game.
   public string itemId;
   public int amount;
}

[System.Serializable]
public class StorySaveData
{
   // Luu ngay hien tai, cac co PlayerPrefs va tien trinh quest/dialogue.
   public int currentDay;
   public StoryFlagSaveData[] flags;
   public StoryIntSaveData[] intValues;
   public QuestProgressSaveData questProgress;
   public DialogueProgressSaveData dialogueProgress;
}

[System.Serializable]
public class StoryFlagSaveData
{
   // Cap key/value cho cac trang thai dung-sai cua story.
   public string key;
   public bool value;
}

[System.Serializable]
public class StoryIntSaveData
{
   // Cap key/value cho cac trang thai dang so nguyen cua story.
   public string key;
   public int value;
}

[System.Serializable]
public class QuestProgressSaveData
{
   // Du lieu phuc hoi DailyQuestManager khi load lai game.
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
   // Du lieu phuc hoi doan hoi thoai dang chay khi nguoi choi save game giua dialogue.
   public bool isDialogueActive;
   public int currentDay;
   public int currentEventId;
   public int currentLineIndex;
}
