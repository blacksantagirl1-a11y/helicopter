using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class DayResetDebug : MonoBehaviour
{
#if UNITY_EDITOR
    private const string ResetDayOnPlayKey = "dialogue.debugResetDayOnPlay";

    [InitializeOnEnterPlayMode]
    private static void  ResetDayOnEnterPlayMode(EnterPlayModeOptions options)
    {
        if (EditorPrefs.GetBool(ResetDayOnPlayKey, false))
        {
            DialogueSaveService.SetCurrentDay(DialogueDay.Day1);
        }
    }
#endif
}
