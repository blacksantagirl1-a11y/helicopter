using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public sealed class DialogueDayBuildReset : IPostprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPostprocessBuild(BuildReport report)
    {
        DialogueNewGameResetService.ResetToDay1();
        Debug.Log($"Reset dialogue current day to {DialogueDay.Day1} after build: {report.summary.outputPath}");
    }
}
