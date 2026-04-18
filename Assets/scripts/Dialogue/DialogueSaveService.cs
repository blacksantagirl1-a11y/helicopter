using UnityEngine;

public static class DialogueSaveService
{
    private const string CurrentDayKey = "dialogue.currentDay";

    public static DialogueDay GetCurrentDay()
    {
        int rawDay = PlayerPrefs.GetInt(CurrentDayKey, (int)DialogueDay.Day1);
        return ClampDay(rawDay);
    }

    public static void SetCurrentDay(DialogueDay day)
    {
        DialogueDay clampedDay = ClampDay((int)day);
        PlayerPrefs.SetInt(CurrentDayKey, (int)clampedDay);
        PlayerPrefs.Save();
    }

    public static DialogueDay AdvanceDay()
    {
        DialogueDay currentDay = GetCurrentDay();
        int nextDay = Mathf.Clamp((int)currentDay + 1, (int)DialogueDay.Day1, (int)DialogueDay.Day6);
        DialogueDay result = ClampDay(nextDay);
        SetCurrentDay(result);
        return result;
    }

    private static DialogueDay ClampDay(int rawDay)
    {
        int clampedValue = Mathf.Clamp(rawDay, (int)DialogueDay.Day1, (int)DialogueDay.Day6);
        return (DialogueDay)clampedValue;
    }
}
