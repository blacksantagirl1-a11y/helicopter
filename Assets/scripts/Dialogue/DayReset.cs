using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class DayResetDebug : MonoBehaviour
{
#if UNITY_EDITOR
    [InitializeOnEnterPlayMode]
    private static void  ResetDayOnEnterPlayMode(EnterPlayModeOptions options)
    {
        DialogueSaveService.SetCurrentDay(DialogueDay.Day1);
        
    }
#endif
}
