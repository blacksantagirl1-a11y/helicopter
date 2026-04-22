using UnityEngine;

// Lop nay chi lo 1 viec: ghi nho "ngay hien tai cua dialogue" vao PlayerPrefs.
// Nhieu script khac co the doc / doi ngay thong qua cac ham static ben duoi.
public static class DialogueSaveService
{
    // Day la ten khoa luu trong bo nho PlayerPrefs cua Unity.
    private const string CurrentDayKey = "dialogue.currentDay";

    // Doc ngay hien tai tu save.
    // Neu chua co save thi mac dinh la Day1.
    public static DialogueDay GetCurrentDay()
    {
        int rawDay = PlayerPrefs.GetInt(CurrentDayKey, (int)DialogueDay.Day1);
        return ClampDay(rawDay);
    }

    // Ghi de ngay hien tai vao save.
    public static void SetCurrentDay(DialogueDay day)
    {
        DialogueDay clampedDay = ClampDay((int)day);
        PlayerPrefs.SetInt(CurrentDayKey, (int)clampedDay);
        PlayerPrefs.Save();
    }

    // Tang ngay len 1 don vi va luu ngay moi.
    public static DialogueDay AdvanceDay()
    {
        DialogueDay currentDay = GetCurrentDay();
        int nextDay = Mathf.Clamp((int)currentDay + 1, (int)DialogueDay.Day1, (int)DialogueDay.Day6);
        DialogueDay result = ClampDay(nextDay);
        SetCurrentDay(result);
        return result;
    }

    // Dam bao gia tri ngay nam trong khoang hop le Day1 -> Day6.
    private static DialogueDay ClampDay(int rawDay)
    {
        int clampedValue = Mathf.Clamp(rawDay, (int)DialogueDay.Day1, (int)DialogueDay.Day6);
        return (DialogueDay)clampedValue;
    }
}
