using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public class CuttingTreeSystem : MonoBehaviour
{
    private const string DefaultPickupPrefabPath = "Assets/model/log/log2.prefab";

    [Header("References")]
    [SerializeField] private ActionScript actionScript;
    [SerializeField] private Camera sourceCamera;
    [SerializeField] private Terrain targetTerrain;
    [SerializeField] private Transform playerRoot;
    [SerializeField] private GameObject pickupPrefab;
    [SerializeField] private LayerMask obstructionMask = Physics.DefaultRaycastLayers;

    [Header("Chop Settings")]
    [SerializeField] private float choppingRange = 7f;
    [SerializeField] private float targetHeight = 2f;
    [SerializeField] private float maxTargetAngle = 12f;
    [SerializeField] private int hitsToCutTree = 3;
    [SerializeField] private string[] cuttablePrototypeKeywords = { "pine", "tree" };

    [Header("Pickup Spawn")]
    [SerializeField] private Vector3 pickupSpawnOffset = new Vector3(0f, 0.15f, 0f);
    [SerializeField] private Vector2 pickupRandomYaw = new Vector2(0f, 360f);

    private readonly Dictionary<string, int> treeHitCounts = new Dictionary<string, int>();
    private ActionScript subscribedActionScript;

    private void Reset()
    {
        TryAutoAssignReferences();
        ClampSerializedValues();
    }

    private void Awake()
    {
        TryAutoAssignReferences();
        ClampSerializedValues();
        RefreshAttackSubscription();
    }

    private void OnEnable()
    {
        TryAutoAssignReferences();
        RefreshAttackSubscription();
    }

    private void OnDisable()
    {
        if (subscribedActionScript != null)
        {
            subscribedActionScript.AttackPerformed -= HandleAttackPerformed;
            subscribedActionScript = null;
        }
    }

    private void OnValidate()
    {
        TryAutoAssignReferences();
        ClampSerializedValues();

        if (Application.isPlaying)
        {
            RefreshAttackSubscription();
        }
    }

    private void HandleAttackPerformed()
    {
        if (!enabled || !gameObject.activeInHierarchy)
        {
            return;
        }

        if (!TryGetTargetTree(out Terrain terrain, out TreeInstance treeInstance, out int treeIndex, out Vector3 treeWorldPosition))
        {
            return;
        }

        string treeKey = BuildTreeKey(treeInstance);
        int nextHitCount = GetHitCount(treeKey) + 1;

        if (nextHitCount >= hitsToCutTree)
        {
            RemoveTree(terrain, treeIndex);
            treeHitCounts.Remove(treeKey);
            SpawnPickup(treeWorldPosition);
            Debug.Log($"Cut down tree '{GetPrototypeName(terrain, treeInstance.prototypeIndex)}'.");
            return;
        }

        treeHitCounts[treeKey] = nextHitCount;
        Debug.Log($"Tree hit {nextHitCount}/{hitsToCutTree}: {GetPrototypeName(terrain, treeInstance.prototypeIndex)}");
    }

    private bool TryGetTargetTree(
        out Terrain terrain,
        out TreeInstance treeInstance,
        out int treeIndex,
        out Vector3 treeWorldPosition)
    {
        terrain = ResolveTerrain();
        treeInstance = default;
        treeIndex = -1;
        treeWorldPosition = Vector3.zero;

        if (terrain == null || sourceCamera == null || terrain.terrainData == null)
        {
            return false;
        }

        TreeInstance[] treeInstances = terrain.terrainData.treeInstances;
        TreePrototype[] prototypes = terrain.terrainData.treePrototypes;
        if (treeInstances == null || treeInstances.Length == 0 || prototypes == null || prototypes.Length == 0)
        {
            return false;
        }

        Vector3 rayOrigin = sourceCamera.transform.position;
        Vector3 rayDirection = sourceCamera.transform.forward;
        float bestScore = float.MaxValue;

        for (int i = 0; i < treeInstances.Length; i++)
        {
            TreeInstance currentTree = treeInstances[i];
            if (!IsTreePrototypeCuttable(prototypes, currentTree.prototypeIndex))
            {
                continue;
            }

            Vector3 worldPosition = GetTreeWorldPosition(terrain, currentTree);
            Vector3 targetPoint = worldPosition + Vector3.up * Mathf.Max(1f, targetHeight * currentTree.heightScale);
            Vector3 toTarget = targetPoint - rayOrigin;
            float distance = toTarget.magnitude;

            if (distance > choppingRange || distance <= Mathf.Epsilon)
            {
                continue;
            }

            float targetAngle = Vector3.Angle(rayDirection, toTarget);
            if (targetAngle > maxTargetAngle)
            {
                continue;
            }

            if (IsViewObstructed(rayOrigin, targetPoint, distance))
            {
                continue;
            }

            float score = (targetAngle * 100f) + distance;
            if (score >= bestScore)
            {
                continue;
            }

            bestScore = score;
            treeInstance = currentTree;
            treeIndex = i;
            treeWorldPosition = worldPosition;
        }

        return treeIndex >= 0;
    }

    private void RemoveTree(Terrain terrain, int treeIndex)
    {
        if (terrain == null || terrain.terrainData == null)
        {
            return;
        }

        TreeInstance[] currentTrees = terrain.terrainData.treeInstances;
        if (currentTrees == null || treeIndex < 0 || treeIndex >= currentTrees.Length)
        {
            return;
        }

        List<TreeInstance> remainingTrees = new List<TreeInstance>(currentTrees);
        remainingTrees.RemoveAt(treeIndex);
        terrain.terrainData.treeInstances = remainingTrees.ToArray();
        terrain.Flush();
    }

    private void SpawnPickup(Vector3 treeWorldPosition)
    {
        if (pickupPrefab == null)
        {
            Debug.LogWarning("Tree chopped but no pickup prefab is assigned on CuttingTreeSystem.");
            return;
        }

        Quaternion pickupRotation = Quaternion.Euler(
            0f,
            Random.Range(pickupRandomYaw.x, pickupRandomYaw.y),
            0f);

        GameObject spawnedPickup = Instantiate(
            pickupPrefab,
            treeWorldPosition + pickupSpawnOffset,
            pickupRotation);

        if (TryGetCombinedRendererBounds(spawnedPickup, out Bounds bounds))
        {
            Vector3 adjustedPosition = treeWorldPosition + pickupSpawnOffset;
            adjustedPosition.y = treeWorldPosition.y + bounds.extents.y + pickupSpawnOffset.y;
            spawnedPickup.transform.position = adjustedPosition;

            EnsurePickupCollider(spawnedPickup, bounds);
        }

        if (spawnedPickup.GetComponent<TreeLogPickup>() == null)
        {
            spawnedPickup.AddComponent<TreeLogPickup>();
        }
    }

    private void EnsurePickupCollider(GameObject pickupObject, Bounds worldBounds)
    {
        if (pickupObject == null || pickupObject.GetComponentInChildren<Collider>() != null)
        {
            return;
        }

        BoxCollider boxCollider = pickupObject.AddComponent<BoxCollider>();
        boxCollider.center = pickupObject.transform.InverseTransformPoint(worldBounds.center);
        boxCollider.size = WorldSizeToLocalSize(pickupObject.transform, worldBounds.size);
    }

    private bool IsTreePrototypeCuttable(TreePrototype[] prototypes, int prototypeIndex)
    {
        if (prototypes == null || prototypeIndex < 0 || prototypeIndex >= prototypes.Length)
        {
            return false;
        }

        if (cuttablePrototypeKeywords == null || cuttablePrototypeKeywords.Length == 0)
        {
            return true;
        }

        GameObject prototypePrefab = prototypes[prototypeIndex].prefab;
        string prototypeName = prototypePrefab != null
            ? prototypePrefab.name
            : string.Empty;

        if (string.IsNullOrWhiteSpace(prototypeName))
        {
            return false;
        }

        string normalizedPrototypeName = prototypeName.ToLowerInvariant();
        for (int i = 0; i < cuttablePrototypeKeywords.Length; i++)
        {
            string keyword = cuttablePrototypeKeywords[i];
            if (string.IsNullOrWhiteSpace(keyword))
            {
                continue;
            }

            if (normalizedPrototypeName.Contains(keyword.ToLowerInvariant()))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsViewObstructed(Vector3 origin, Vector3 targetPoint, float distanceToTarget)
    {
        Vector3 direction = targetPoint - origin;
        if (Physics.Raycast(
                origin,
                direction.normalized,
                out RaycastHit hit,
                distanceToTarget,
                obstructionMask,
                QueryTriggerInteraction.Ignore))
        {
            if (playerRoot != null && hit.transform.IsChildOf(playerRoot))
            {
                return false;
            }

            if (targetTerrain != null && hit.transform == targetTerrain.transform)
            {
                return false;
            }

            return true;
        }

        return false;
    }

    private Terrain ResolveTerrain()
    {
        if (targetTerrain != null)
        {
            return targetTerrain;
        }

        targetTerrain = Terrain.activeTerrain;
        if (targetTerrain == null)
        {
            targetTerrain = FindFirstObjectByType<Terrain>();
        }

        return targetTerrain;
    }

    private void TryAutoAssignReferences()
    {
        actionScript ??= GetComponent<ActionScript>();
        actionScript ??= GetComponentInParent<ActionScript>();
        actionScript ??= FindFirstObjectByType<ActionScript>();

        sourceCamera ??= GetComponent<Camera>();
        sourceCamera ??= GetComponentInChildren<Camera>();
        sourceCamera ??= Camera.main;

        if (playerRoot == null && actionScript != null)
        {
            playerRoot = actionScript.transform;
        }

        targetTerrain ??= Terrain.activeTerrain;
        targetTerrain ??= FindFirstObjectByType<Terrain>();

#if UNITY_EDITOR
        if (pickupPrefab == null)
        {
            pickupPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DefaultPickupPrefabPath);
        }
#endif
    }

    private void RefreshAttackSubscription()
    {
        if (subscribedActionScript == actionScript)
        {
            return;
        }

        if (subscribedActionScript != null)
        {
            subscribedActionScript.AttackPerformed -= HandleAttackPerformed;
        }

        subscribedActionScript = actionScript;

        if (subscribedActionScript != null)
        {
            subscribedActionScript.AttackPerformed += HandleAttackPerformed;
        }
    }

    private void ClampSerializedValues()
    {
        choppingRange = Mathf.Max(1f, choppingRange);
        targetHeight = Mathf.Max(0.5f, targetHeight);
        maxTargetAngle = Mathf.Clamp(maxTargetAngle, 1f, 45f);
        hitsToCutTree = Mathf.Max(1, hitsToCutTree);
    }

    private int GetHitCount(string treeKey)
    {
        if (string.IsNullOrWhiteSpace(treeKey))
        {
            return 0;
        }

        return treeHitCounts.TryGetValue(treeKey, out int hitCount)
            ? hitCount
            : 0;
    }

    private static string BuildTreeKey(TreeInstance treeInstance)
    {
        return string.Concat(
            treeInstance.prototypeIndex,
            "_",
            Mathf.RoundToInt(treeInstance.position.x * 10000f),
            "_",
            Mathf.RoundToInt(treeInstance.position.y * 10000f),
            "_",
            Mathf.RoundToInt(treeInstance.position.z * 10000f));
    }

    private static Vector3 GetTreeWorldPosition(Terrain terrain, TreeInstance treeInstance)
    {
        Vector3 terrainPosition = terrain.GetPosition();
        Vector3 terrainSize = terrain.terrainData.size;
        Vector3 worldPosition = terrainPosition + Vector3.Scale(treeInstance.position, terrainSize);
        worldPosition.y = terrain.SampleHeight(worldPosition) + terrainPosition.y;
        return worldPosition;
    }

    private static string GetPrototypeName(Terrain terrain, int prototypeIndex)
    {
        if (terrain == null || terrain.terrainData == null)
        {
            return "Unknown";
        }

        TreePrototype[] prototypes = terrain.terrainData.treePrototypes;
        if (prototypes == null || prototypeIndex < 0 || prototypeIndex >= prototypes.Length)
        {
            return "Unknown";
        }

        return prototypes[prototypeIndex].prefab != null
            ? prototypes[prototypeIndex].prefab.name
            : "Unknown";
    }

    private static bool TryGetCombinedRendererBounds(GameObject target, out Bounds bounds)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            bounds = default;
            return false;
        }

        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return true;
    }

    private static Vector3 WorldSizeToLocalSize(Transform targetTransform, Vector3 worldSize)
    {
        Vector3 lossyScale = targetTransform.lossyScale;

        return new Vector3(
            worldSize.x / Mathf.Max(0.0001f, Mathf.Abs(lossyScale.x)),
            worldSize.y / Mathf.Max(0.0001f, Mathf.Abs(lossyScale.y)),
            worldSize.z / Mathf.Max(0.0001f, Mathf.Abs(lossyScale.z)));
    }
}
