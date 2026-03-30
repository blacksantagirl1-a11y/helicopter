using UnityEngine;
using UnityEngine.SceneManagement;

public class RunnerGameManager : MonoBehaviour
{
    [Header("Run Duration (seconds)")]
    [Min(10f)] public float targetDurationSeconds = 90f; // ~1-2 minutes by default

    [Header("Speed")]
    [Min(1f)] public float worldSpeed = 8f;
    [Min(0f)] public float speedRampPerSecond = 0.03f;
    [Min(0f)] public float maxWorldSpeed = 14f;

    [Header("State")]
    public bool autoStart = true;

    public float ElapsedSeconds { get; private set; }
    public bool IsRunning { get; private set; }
    public bool IsFinished { get; private set; }
    public bool IsGameOver { get; private set; }

    public float CurrentWorldSpeed
    {
        get
        {
            if (!IsRunning) return 0f;
            float s = worldSpeed + ElapsedSeconds * speedRampPerSecond;
            return Mathf.Min(s, maxWorldSpeed);
        }
    }

    void Start()
    {
        if (autoStart) StartRun();
    }

    void Update()
    {
        // Manual start if autoStart is off
        if (!IsRunning && !IsFinished && !IsGameOver && !autoStart)
        {
            if (Input.GetKeyDown(KeyCode.Space))
                StartRun();
        }

        if (IsRunning)
        {
            ElapsedSeconds += Time.deltaTime;
            if (ElapsedSeconds >= targetDurationSeconds)
            {
                ElapsedSeconds = targetDurationSeconds;
                Finish();
            }
        }

        if (IsGameOver || IsFinished)
        {
            if (Input.GetKeyDown(KeyCode.R))
                ReloadScene();
        }
    }

    public void StartRun()
    {
        IsRunning = true;
        IsFinished = false;
        IsGameOver = false;
        ElapsedSeconds = 0f;
    }

    public void GameOver()
    {
        if (IsGameOver || IsFinished) return;
        IsGameOver = true;
        IsRunning = false;
    }

    public void Finish()
    {
        if (IsFinished || IsGameOver) return;
        IsFinished = true;
        IsRunning = false;
    }

    void ReloadScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.buildIndex);
    }
}

