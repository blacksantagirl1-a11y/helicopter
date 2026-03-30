using UnityEngine;

public class RunnerObstacle : MonoBehaviour
{
    public RunnerGameManager game;
    [Min(0f)] public float extraSpeed = 0f; // optional: some obstacles move slightly faster
    public float destroyZ = -15f;

    void Awake()
    {
        if (!game) game = FindRunnerGameManager();
    }

    void Update()
    {
        if (!game || !game.IsRunning) return;

        float speed = game.CurrentWorldSpeed + extraSpeed;
        transform.position += Vector3.back * (speed * Time.deltaTime);

        if (transform.position.z <= destroyZ)
            Destroy(gameObject);
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

