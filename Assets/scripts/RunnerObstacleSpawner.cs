using UnityEngine;

public class RunnerObstacleSpawner : MonoBehaviour
{
    [Header("Refs")]
    public RunnerGameManager game;

    [Header("Prefabs")]
    public GameObject[] obstaclePrefabs;

    [Header("Spawn")]
    public float spawnZ = 25f;
    public float minGapZ = 9f;
    public float maxGapZ = 14f;
    [Range(0f, 1f)] public float twoObstaclesChance = 0.25f;

    [Header("Lanes")]
    [Min(0.5f)] public float laneOffset = 2f;

    float _distanceUntilNextSpawn;

    void Awake()
    {
        if (!game) game = FindRunnerGameManager();
        _distanceUntilNextSpawn = Random.Range(minGapZ, maxGapZ);
    }

    void Update()
    {
        if (!game || !game.IsRunning) return;
        if (obstaclePrefabs == null || obstaclePrefabs.Length == 0) return;

        // Spawn based on "distance" passed to keep obstacle spacing consistent as speed ramps.
        _distanceUntilNextSpawn -= game.CurrentWorldSpeed * Time.deltaTime;
        if (_distanceUntilNextSpawn <= 0f)
        {
            SpawnBatch();
            _distanceUntilNextSpawn = Random.Range(minGapZ, maxGapZ);
        }
    }

    void SpawnBatch()
    {
        int lane1 = Random.Range(-1, 2);
        SpawnOne(lane1);

        if (Random.value < twoObstaclesChance)
        {
            int lane2 = lane1;
            int safety = 0;
            while (lane2 == lane1 && safety++ < 10)
                lane2 = Random.Range(-1, 2);
            SpawnOne(lane2);
        }
    }

    void SpawnOne(int lane)
    {
        GameObject prefab = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)];
        Vector3 pos = new Vector3(lane * laneOffset, 0f, spawnZ);
        GameObject go = Instantiate(prefab, pos, Quaternion.identity, transform);

        // Ensure it moves
        if (!go.GetComponent<RunnerObstacle>())
            go.AddComponent<RunnerObstacle>();
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

