using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
// CuttingTreeSystem xu ly co che chat cay tren Terrain.
// Luong chinh:
// 1. Nghe su kien AttackPerformed tu ActionScript.
// 2. Moi cu danh, tim cay hop le dang o truoc camera.
// 3. Cong so hit cho cay do.
// 4. Du hit thi xoa cay khoi terrain va spawn ra log de nhat.
public class CuttingTreeSystem : MonoBehaviour
{
    private const string DefaultPickupPrefabPath = "Assets/model/log/log2.prefab";

    [Header("References")]
    [Tooltip("Script hành động để nhận sự kiện tấn công")]
    [SerializeField] private ActionScript actionScript;
    [Tooltip("Camera dùng raycast để ngắm cây")]
    [SerializeField] private Camera sourceCamera;
    [Tooltip("Terrain chứa cây sẽ bị chặt")]
    [SerializeField] private Terrain targetTerrain;
    [Tooltip("Root player để bỏ qua va chạm tự thân khi raycast")]
    [SerializeField] private Transform playerRoot;
    [Tooltip("Prefab log spawn ra sau khi chặt cây")]
    [SerializeField] private GameObject pickupPrefab;
    [Tooltip("Layer mask kiểm tra vật cản giữa camera và cây")]
    [SerializeField] private LayerMask obstructionMask = Physics.DefaultRaycastLayers;

    [Header("Chop Settings")]
    [Tooltip("Khoảng cách chặt cây tối đa")]
    [SerializeField] private float choppingRange = 7f;
    [Tooltip("Độ cao điểm ngắm mục tiêu trên thân cây")]
    [SerializeField] private float targetHeight = 2f;
    [Tooltip("Góc lệch tối đa giữa hướng nhìn và cây")]
    [SerializeField] private float maxTargetAngle = 12f;
    [Tooltip("Số hit cần để đốn một cây")]
    [SerializeField] private int hitsToCutTree = 3;
    [Tooltip("Từ khóa tên prototype cây được phép chặt")]
    [SerializeField] private string[] cuttablePrototypeKeywords = { "pine", "tree" };

    [Header("Pickup Spawn")]
    [Tooltip("Offset vị trí spawn log so với gốc cây")]
    [SerializeField] private Vector3 pickupSpawnOffset = new Vector3(0f, 0.15f, 0f);
    [Tooltip("Khoảng random góc xoay Y của log sau khi spawn")]
    [SerializeField] private Vector2 pickupRandomYaw = new Vector2(0f, 360f);

    [Header("After Cut")]
    [Tooltip("Giu lai mot proxy vo hinh tu prefab cua cay de khong mat het collider/trigger sau khi chat")]
    [SerializeField] private bool preservePrototypeProxyAfterCut = true;
    [Tooltip("Doi collider tren proxy thanh trigger de khong chan duong sau khi cay bien mat")]
    [SerializeField] private bool convertProxyCollidersToTriggers = true;

    // Nho xem moi cay da bi chat bao nhieu lan.
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

    // Duoc goi moi khi don danh den thoi diem tinh hit.
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
        PlayChopTreeSound();

        if (nextHitCount >= hitsToCutTree)
        {
            PlayTreeFallSound();
            SpawnPrototypeProxyAfterCut(terrain, treeInstance, treeWorldPosition);
            RemoveTree(terrain, treeIndex);
            treeHitCounts.Remove(treeKey);
            SpawnPickup(treeWorldPosition);
            Debug.Log($"Cut down tree '{GetPrototypeName(terrain, treeInstance.prototypeIndex)}'.");
            return;
        }

        treeHitCounts[treeKey] = nextHitCount;
        Debug.Log($"Tree hit {nextHitCount}/{hitsToCutTree}: {GetPrototypeName(terrain, treeInstance.prototypeIndex)}");
    }

    private static void PlayChopTreeSound()
    {
        SoundManager soundManager = ResolveSoundManager();
        PlayOneShot(soundManager != null ? soundManager.chopTreeSource : null);
    }

    private static void PlayTreeFallSound()
    {
        SoundManager soundManager = ResolveSoundManager();
        PlayOneShot(soundManager != null ? soundManager.treeFallSource : null);
    }

    private static SoundManager ResolveSoundManager()
    {
        return SoundManager.Instance != null
            ? SoundManager.Instance
            : FindFirstObjectByType<SoundManager>();
    }

    private static void PlayOneShot(AudioSource audioSource)
    {
        if (audioSource == null)
        {
            return;
        }

        if (audioSource.clip != null)
        {
            audioSource.PlayOneShot(audioSource.clip);
            return;
        }

        audioSource.Play();
    }

    // Thu tim cay dang duoc nham toi.
    // Cay hop le phai:
    // - nam trong tam
    // - nam trong goc nhin cho phep
    // - dung loai cay co the chat
    // - khong bi vat can che
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

    // Xoa cay khoi danh sach treeInstances cua terrain.
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

    // Spawn khuc go sau khi cay bi chat do.
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
        }

        EnsurePickupCollider(spawnedPickup);

        if (spawnedPickup.GetComponent<TreeLogPickup>() == null)
        {
            spawnedPickup.AddComponent<TreeLogPickup>();
        }
    }

    // Dam bao pickup co collider de co the duoc nhin va nhat.
    private void EnsurePickupCollider(GameObject pickupObject)
    {
        if (pickupObject == null)
        {
            return;
        }

        Collider[] existingColliders = pickupObject.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < existingColliders.Length; i++)
        {
            Collider existingCollider = existingColliders[i];
            if (existingCollider == null)
            {
                continue;
            }

            existingCollider.enabled = true;
            return;
        }

        if (!TryGetCombinedRendererBounds(pickupObject, out Bounds worldBounds))
        {
            return;
        }

        BoxCollider boxCollider = pickupObject.AddComponent<BoxCollider>();
        boxCollider.center = pickupObject.transform.InverseTransformPoint(worldBounds.center);
        boxCollider.size = WorldSizeToLocalSize(pickupObject.transform, worldBounds.size);
    }

    // Sau khi xoa TreeInstance, tao mot proxy vo hinh de giu lai collider/trigger cua prefab goc.
    private void SpawnPrototypeProxyAfterCut(Terrain terrain, TreeInstance treeInstance, Vector3 treeWorldPosition)
    {
        if (!preservePrototypeProxyAfterCut)
        {
            return;
        }

        GameObject prototypePrefab = GetPrototypePrefab(terrain, treeInstance.prototypeIndex);
        if (prototypePrefab == null)
        {
            return;
        }

        Quaternion proxyRotation = Quaternion.Euler(0f, treeInstance.rotation * Mathf.Rad2Deg, 0f);
        GameObject proxy = Instantiate(prototypePrefab, treeWorldPosition, proxyRotation);
        proxy.name = $"{prototypePrefab.name}_CutProxy";

        Vector3 proxyScale = proxy.transform.localScale;
        proxy.transform.localScale = new Vector3(
            proxyScale.x * Mathf.Max(0.01f, treeInstance.widthScale),
            proxyScale.y * Mathf.Max(0.01f, treeInstance.heightScale),
            proxyScale.z * Mathf.Max(0.01f, treeInstance.widthScale));

        Collider[] colliders = proxy.GetComponentsInChildren<Collider>(true);
        if (colliders.Length == 0)
        {
            Destroy(proxy);
            return;
        }

        Renderer[] renderers = proxy.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].enabled = false;
            }
        }

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider collider = colliders[i];
            if (collider == null)
            {
                continue;
            }

            collider.enabled = true;
            if (convertProxyCollidersToTriggers)
            {
                collider.isTrigger = true;
            }
        }

    }

    // Chi cho phep chat nhung cay co ten prototype khop voi tu khoa cau hinh.
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

    // Neu giua camera va cay co vat can thi bo qua cay do.
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

    // Tu tim terrain dang duoc dung trong scene.
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

    // Co gang auto gan cac reference can thiet.
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

    // Dang ky / huy dang ky nghe event AttackPerformed tu ActionScript.
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

    // Khoa cac gia tri config ve mien hop le.
    private void ClampSerializedValues()
    {
        choppingRange = Mathf.Max(1f, choppingRange);
        targetHeight = Mathf.Max(0.5f, targetHeight);
        maxTargetAngle = Mathf.Clamp(maxTargetAngle, 1f, 45f);
        hitsToCutTree = Mathf.Max(1, hitsToCutTree);
    }

    // Lay so hit hien tai cua cay.
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

    // Tao key gan nhu duy nhat cho tung cay de luu hit count rieng.
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

    // Doi vi tri normalized cua TreeInstance thanh vi tri world that.
    private static Vector3 GetTreeWorldPosition(Terrain terrain, TreeInstance treeInstance)
    {
        Vector3 terrainPosition = terrain.GetPosition();
        Vector3 terrainSize = terrain.terrainData.size;
        Vector3 worldPosition = terrainPosition + Vector3.Scale(treeInstance.position, terrainSize);
        worldPosition.y = terrain.SampleHeight(worldPosition) + terrainPosition.y;
        return worldPosition;
    }

    // Lay ten prefab goc cua prototype cay, chu yeu de log / debug.
    private static string GetPrototypeName(Terrain terrain, int prototypeIndex)
    {
        GameObject prototypePrefab = GetPrototypePrefab(terrain, prototypeIndex);
        return prototypePrefab != null
            ? prototypePrefab.name
            : "Unknown";
    }

    private static GameObject GetPrototypePrefab(Terrain terrain, int prototypeIndex)
    {
        if (terrain == null || terrain.terrainData == null)
        {
            return null;
        }

        TreePrototype[] prototypes = terrain.terrainData.treePrototypes;
        if (prototypes == null || prototypeIndex < 0 || prototypeIndex >= prototypes.Length)
        {
            return null;
        }

        return prototypes[prototypeIndex].prefab;
    }

    // Gom bounds cua cac renderer con lai thanh 1 bounds chung.
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

    // Doi kich thuoc world space ve local space cua object.
    private static Vector3 WorldSizeToLocalSize(Transform targetTransform, Vector3 worldSize)
    {
        Vector3 lossyScale = targetTransform.lossyScale;

        return new Vector3(
            worldSize.x / Mathf.Max(0.0001f, Mathf.Abs(lossyScale.x)),
            worldSize.y / Mathf.Max(0.0001f, Mathf.Abs(lossyScale.y)),
            worldSize.z / Mathf.Max(0.0001f, Mathf.Abs(lossyScale.z)));
    }
}
