using UnityEngine;

public static class DialogueNewGameResetService
{
    public static void ResetToDay1()
    {
        const DialogueDay resetDay = DialogueDay.Day1;

        SimplePickup.StartNewPersistentPickupCampaign(resetDay);
        DailyQuestManager.ResetDay5DataPackageForReplay();
        DailyQuestManager.ResetDay6EscapeForReplay();
        DialogueSaveService.SetCurrentDay(resetDay);

        Debug.Log($"Dialogue current day set to {resetDay}.");
    }
}
