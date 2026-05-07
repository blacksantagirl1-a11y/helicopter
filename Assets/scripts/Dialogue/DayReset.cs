using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class DayResetDebug : MonoBehaviour
{
    [SerializeField] private DialogueDay editorDay = DialogueDay.Day1;

#if UNITY_EDITOR
    private const string ResetDayOnPlayKey = "dialogue.debugResetDayOnPlay";

    [ContextMenu("Dialogue/Apply Selected Day In Editor")]
    private void ApplySelectedDayInEditor()
    {
        SetDayInEditor(editorDay);
    }

    [ContextMenu("Dialogue/Reset To Day1 In Editor")]
    private void ResetToDay1InEditor()
    {
        SetDayInEditor(DialogueDay.Day1);
    }

    [MenuItem("Tools/Dialogue/Reset Current Day To Day1")]
    private static void ResetCurrentDayToDay1Menu()
    {
        SetDayInEditor(DialogueDay.Day1);
    }

    [InitializeOnEnterPlayMode]
    private static void ResetDayOnEnterPlayMode(EnterPlayModeOptions options)
    {
        if (EditorPrefs.GetBool(ResetDayOnPlayKey, false))
        {
            SetDayInEditor(DialogueDay.Day1);
        }
    }

    private static void SetDayInEditor(DialogueDay day)
    {
        if (day == DialogueDay.Day1)
        {
            SimplePickup.StartNewPersistentPickupCampaign(day);
        }

        DialogueSaveService.SetCurrentDay(day);
        Debug.Log($"Dialogue current day set to {day}.");
    }
#endif
}
