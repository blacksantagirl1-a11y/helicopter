using UnityEngine;

public class RunnerHUD : MonoBehaviour
{
    public RunnerGameManager game;
    public GUIStyle style;

    void Awake()
    {
        if (!game) game = FindRunnerGameManager();
        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                alignment = TextAnchor.UpperLeft
            };
            style.normal.textColor = Color.white;
        }
    }

    void OnGUI()
    {
        if (!game) return;

        float remaining = Mathf.Max(0f, game.targetDurationSeconds - game.ElapsedSeconds);
        string line1 = $"Time: {remaining:0.0}s";
        string line2 = game.IsFinished ? "FINISH! Press R to restart" :
                      game.IsGameOver ? "GAME OVER! Press R to restart" :
                      "A/D or ←/→ to move lane, Space to jump";

        GUI.Label(new Rect(16, 16, 900, 30), line1, style);
        GUI.Label(new Rect(16, 46, 900, 30), line2, style);
    }

    static RunnerGameManager FindRunnerGameManager()
    {
#if UNITY_2023_1_OR_NEWER
        return FindFirstObjectByType<RunnerGameManager>();
#else
        return FindObjectOfType<RunnerGameManager>();
#endif
    }
}

