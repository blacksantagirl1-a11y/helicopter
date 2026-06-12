using UnityEngine;

public static class ExhaustionDayResetService
{
    public static DialogueDay ResetToStartOfCurrentDay()
    {
        DialogueDay currentDay = DialogueSaveService.GetCurrentDay();

        SimplePickup.StartNewPersistentPickupCampaign(currentDay);
        ResetDaySpecificState(currentDay);
        DialogueSaveService.SetCurrentDay(currentDay);

        return currentDay;
    }

    private static void ResetDaySpecificState(DialogueDay currentDay)
    {
        if (currentDay == DialogueDay.Day5)
        {
            DailyQuestManager.ResetDay5DataPackageForReplay();
        }
        else if (currentDay == DialogueDay.Day6)
        {
            DailyQuestManager.ResetDay6EscapeForReplay();
        }
    }
}
