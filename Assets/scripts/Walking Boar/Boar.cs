using UnityEngine;
using UnityEngine.AI;

#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public class Boar : MonoBehaviour
{
    private const string WalkingAroundParameterName = "WalkingAround";
    private const string DefaultMeatPickupAssetPath = "Assets/Resources/Inventory/MeatPickup.prefab";
    private const string DefaultMeatPickupResourcePath = "Inventory/MeatPickup";

    public static event System.Action<Boar> BoarKilled;

    [Header("References")]
    [Tooltip("Script hành động của player để nhận sự kiện tấn công")]
    [SerializeField] private ActionScript actionScript;
    [Tooltip("Camera dùng để xác định mục tiêu tấn công")]
    [SerializeField] private Camera sourceCamera;
    [Tooltip("Root player để loại trừ va chạm tự thân")]
    [SerializeField] private Transform playerRoot;
    [Tooltip("Prefab thịt rơi ra khi heo chết")]
    [SerializeField] private GameObject meatPickupPrefab;
    [Tooltip("Layer mask vật cản giữa camera và heo")]
    [SerializeField] private LayerMask obstructionMask = Physics.DefaultRaycastLayers;
    [Tooltip("Layer mask mặt đất để canh vị trí di chuyển")]
    [SerializeField] private LayerMask groundMask = Physics.DefaultRaycastLayers;

    [Header("Combat")]
    [Tooltip("Số hit cần để hạ gục heo")]
    [SerializeField]
    [Min(1)]
    private int hitsToKill = 5;
    [Tooltip("Tầm tối đa để đòn đánh chạm vào heo")]
    [SerializeField]
    [Min(0.5f)]
    private float attackRange = 4.5f;
    [Tooltip("Nới rộng bounds mục tiêu để dễ trúng khi ngắm")]
    [SerializeField]
    [Min(0f)]
    private float targetBoundsPadding = 0.35f;

    [Header("Roaming")]
    [Tooltip("Bán kính khu vực heo đi lang thang")]
    [SerializeField]
    [Min(1f)]
    private float roamRadius = 8f;
    [Tooltip("Khoảng thời gian nghỉ ngẫu nhiên giữa các lần di chuyển")]
    [SerializeField] private Vector2 idleDurationRange = new Vector2(1f, 2.4f);
    [Tooltip("Ngưỡng khoảng cách coi là đã đến điểm đích")]
    [SerializeField]
    [Min(0.1f)]
    private float destinationTolerance = 0.4f;

    [Header("Movement")]
    [Tooltip("Tốc độ di chuyển của heo")]
    [SerializeField]
    [Min(0.1f)]
    private float moveSpeed = 1.85f;
    [Tooltip("Tốc độ xoay mặt theo hướng di chuyển")]
    [SerializeField]
    [Min(0.1f)]
    private float rotationSpeed = 8f;
    [Tooltip("Khoảng cách tìm điểm NavMesh hợp lệ")]
    [SerializeField]
    [Min(0.5f)]
    private float navMeshSampleDistance = 5f;
    [Tooltip("Độ cao bắt đầu raycast dò mặt đất")]
    [SerializeField]
    [Min(0.25f)]
    private float groundProbeHeight = 4f;
    [Tooltip("Chiều dài raycast dò mặt đất")]
    [SerializeField]
    [Min(1f)]
    private float groundProbeDistance = 16f;

    [Header("Drop")]
    [Tooltip("Offset vị trí rơi vật phẩm khi heo chết")]
    [SerializeField] private Vector3 dropOffset = new Vector3(0f, 0.15f, 0f);
    [Tooltip("Khoảng random góc xoay Y của vật phẩm rơi")]
    [SerializeField] private Vector2 dropRandomYaw = new Vector2(0f, 360f);

    private ActionScript subscribedActionScript;
    private Animator animator;
    private NavMeshAgent navMeshAgent;
    private int walkingAroundHash;
    private bool hasWalkingAroundParameter;
    private int currentHealth;
    private Vector3 roamCenter;
    private Vector3 currentDestination;
    private float idleTimer;
    private bool hasDestination;
    private bool isDead;

    private void Reset()
    {
        TryAutoAssignReferences();
        ClampSerializedValues();
        CacheAnimatorData();
    }

    private void Awake()
    {
        TryAutoAssignReferences();
        ClampSerializedValues();
        CacheAnimatorData();

        // Spawn-time state: remember the home area and reset health from the serialized setup.
        currentHealth = hitsToKill;
        roamCenter = GetInitialRoamCenter();
        transform.position = GetGroundAlignedPosition(transform.position);

        if (navMeshAgent != null)
        {
            navMeshAgent.updateRotation = false;
            navMeshAgent.speed = moveSpeed;
        }

        RefreshAttackSubscription();
        SetWalkingState(false);
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

        SetWalkingState(false);
    }

    private void OnValidate()
    {
        TryAutoAssignReferences();
        ClampSerializedValues();
        CacheAnimatorData();

#if UNITY_EDITOR
        if (meatPickupPrefab == null)
        {
            meatPickupPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DefaultMeatPickupAssetPath);
        }
#endif

        if (!Application.isPlaying)
        {
            return;
        }

        RefreshAttackSubscription();
    }

    private void Update()
    {
        if (isDead)
        {
            return;
        }

        if (CanUseNavMesh())
        {
            UpdateNavMeshMovement();
            return;
        }

        UpdateTransformMovement();
    }

    private void HandleAttackPerformed()
    {
        if (!enabled || !gameObject.activeInHierarchy || isDead)
        {
            return;
        }

        TryAutoAssignReferences();

        // The boar listens to the player's attack event, then validates whether the
        // center-screen attack ray is actually pointing at this boar and not blocked.
        Camera attackCamera = ResolveSourceCamera();
        if (attackCamera == null)
        {
            return;
        }

        if (!TryGetHitDistance(attackCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f)), out float hitDistance))
        {
            return;
        }

        if (hitDistance > attackRange)
        {
            return;
        }

        if (Physics.Raycast(
                attackCamera.transform.position,
                attackCamera.transform.forward,
                out RaycastHit hit,
                hitDistance,
                obstructionMask,
                QueryTriggerInteraction.Ignore) &&
            hit.transform != null &&
            !hit.transform.IsChildOf(transform))
        {
            return;
        }

        TakeDamage();
    }

    private void TakeDamage()
    {
        currentHealth = Mathf.Max(0, currentHealth - 1);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;
        SetWalkingState(false);

        if (navMeshAgent != null && navMeshAgent.enabled && navMeshAgent.isOnNavMesh)
        {
            navMeshAgent.ResetPath();
        }

        ConsumeKillStamina();
        PlayKillBoarSound();
        BoarKilled?.Invoke(this);
        SpawnMeatPickup();
        Destroy(gameObject);
    }

    private void ConsumeKillStamina()
    {
        Stamina stamina = FindFirstObjectByType<Stamina>();
        if (stamina != null)
        {
            stamina.ConsumeBoarKillStamina();
        }
    }

    private static void PlayKillBoarSound()
    {
        SoundManager soundManager = ResolveSoundManager();
        PlayOneShot(soundManager != null ? soundManager.killBoarSource : null);
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

    private void UpdateNavMeshMovement()
    {
        navMeshAgent.speed = moveSpeed;

        // Pause briefly between roam points so the boar feels less mechanical.
        if (idleTimer > 0f)
        {
            idleTimer -= Time.deltaTime;
            navMeshAgent.ResetPath();
            SetWalkingState(false);
            return;
        }

        if (!hasDestination)
        {
            QueueRandomRoamDestination();
        }

        if (!hasDestination)
        {
            SetWalkingState(false);
            return;
        }

        navMeshAgent.SetDestination(currentDestination);

        Vector3 desiredVelocity = navMeshAgent.desiredVelocity;
        desiredVelocity.y = 0f;
        RotateTowards(desiredVelocity);

        bool reachedDestination = !navMeshAgent.pathPending &&
            navMeshAgent.remainingDistance <= Mathf.Max(destinationTolerance, navMeshAgent.stoppingDistance + 0.05f);

        if (reachedDestination)
        {
            hasDestination = false;
            idleTimer = Random.Range(idleDurationRange.x, idleDurationRange.y);
            navMeshAgent.ResetPath();
            SetWalkingState(false);
            return;
        }

        SetWalkingState(navMeshAgent.velocity.sqrMagnitude > 0.01f || desiredVelocity.sqrMagnitude > 0.01f);
    }

    private void UpdateTransformMovement()
    {
        // Fallback movement path when the boar is not standing on a valid NavMesh.
        if (idleTimer > 0f)
        {
            idleTimer -= Time.deltaTime;
            SetWalkingState(false);
            return;
        }

        if (!hasDestination)
        {
            QueueRandomRoamDestination();
        }

        if (!hasDestination)
        {
            SetWalkingState(false);
            return;
        }

        Vector3 nextPosition = transform.position;
        Vector3 toDestination = currentDestination - nextPosition;
        toDestination.y = 0f;

        if (toDestination.sqrMagnitude <= destinationTolerance * destinationTolerance)
        {
            transform.position = GetGroundAlignedPosition(currentDestination);
            hasDestination = false;
            idleTimer = Random.Range(idleDurationRange.x, idleDurationRange.y);
            SetWalkingState(false);
            return;
        }
        Vector3 movement = toDestination.normalized * moveSpeed * Time.deltaTime;
        if (movement.sqrMagnitude > toDestination.sqrMagnitude)
        {
            movement = toDestination;
        }

        nextPosition += movement;
        transform.position = GetGroundAlignedPosition(nextPosition);
        RotateTowards(movement);
        SetWalkingState(true);
    }

    private void QueueRandomRoamDestination()
    {
        const int maxAttempts = 8;

        // Keep trying random points inside the roam circle until one lands on valid ground/NavMesh.
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            Vector2 randomOffset = Random.insideUnitCircle * roamRadius;
            Vector3 destination = roamCenter + new Vector3(randomOffset.x, 0f, randomOffset.y);
            if (!TryResolveDestination(destination, out Vector3 resolvedDestination))
            {
                continue;
            }

            currentDestination = resolvedDestination;
            hasDestination = true;
            return;
        }

        idleTimer = Random.Range(idleDurationRange.x, idleDurationRange.y);
    }

    private bool TryResolveDestination(Vector3 desiredDestination, out Vector3 resolvedDestination)
    {
        desiredDestination = ClampToRoamArea(desiredDestination);

        if (CanUseNavMesh())
        {
            if (NavMesh.SamplePosition(desiredDestination, out NavMeshHit hit, navMeshSampleDistance, NavMesh.AllAreas))
            {
                resolvedDestination = hit.position;
                return true;
            }

            resolvedDestination = Vector3.zero;
            return false;
        }

        resolvedDestination = GetGroundAlignedPosition(desiredDestination);
        return true;
    }

    private bool TryGetHitDistance(Ray ray, out float hitDistance)
    {
        hitDistance = float.MaxValue;
        bool foundTarget = false;

        // Use expanded bounds so the player does not need to click a tiny collider exactly.
        Collider[] colliders = GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider collider = colliders[i];
            if (collider == null || !collider.enabled)
            {
                continue;
            }

            Bounds colliderBounds = collider.bounds;
            colliderBounds.Expand(targetBoundsPadding);

            if (!colliderBounds.IntersectRay(ray, out float colliderDistance))
            {
                continue;
            }

            if (colliderDistance < hitDistance)
            {
                hitDistance = colliderDistance;
                foundTarget = true;
            }
        }

        if (!TryGetCombinedBounds(gameObject, out Bounds combinedBounds))
        {
            return foundTarget;
        }

        combinedBounds.Expand(targetBoundsPadding);
        if (!combinedBounds.IntersectRay(ray, out float boundsDistance))
        {
            return foundTarget;
        }

        if (!foundTarget || boundsDistance < hitDistance)
        {
            hitDistance = boundsDistance;
            foundTarget = true;
        }

        return foundTarget;
    }

    private void SpawnMeatPickup()
    {
        GameObject pickupPrefab = ResolvePickupPrefab();
        if (pickupPrefab == null)
        {
            Debug.LogWarning("Boar died but no meat pickup prefab could be found.");
            return;
        }

        // Drop the pickup onto the ground and make sure it has a collider for interaction.
        Quaternion rotation = Quaternion.Euler(
            0f,
            Random.Range(dropRandomYaw.x, dropRandomYaw.y),
            0f);

        Vector3 spawnPosition = GetGroundAlignedPosition(transform.position + dropOffset);
        GameObject spawnedPickup = Instantiate(pickupPrefab, spawnPosition, rotation);

        if (spawnedPickup.GetComponent<MeatPickup>() == null)
        {
            spawnedPickup.AddComponent<MeatPickup>();
        }

        if (TryGetCombinedBounds(spawnedPickup, out Bounds pickupBounds))
        {
            Vector3 adjustedPosition = spawnPosition;
            adjustedPosition.y += pickupBounds.extents.y;
            spawnedPickup.transform.position = adjustedPosition;
            EnsurePickupCollider(spawnedPickup, pickupBounds);
        }
    }

    private GameObject ResolvePickupPrefab()
    {
        if (meatPickupPrefab != null)
        {
            return meatPickupPrefab;
        }

        meatPickupPrefab = Resources.Load<GameObject>(DefaultMeatPickupResourcePath);
        if (meatPickupPrefab != null)
        {
            return meatPickupPrefab;
        }

#if UNITY_EDITOR
        meatPickupPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DefaultMeatPickupAssetPath);
#endif

        return meatPickupPrefab;
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

    private void TryAutoAssignReferences()
    {
        actionScript ??= GetComponent<ActionScript>();
        actionScript ??= GetComponentInParent<ActionScript>();
        actionScript ??= FindFirstObjectByType<ActionScript>();

        // Camera.main is not always the active gameplay camera in this project,
        // so we re-resolve it instead of trusting a stale reference.
        sourceCamera = ResolveSourceCamera();

        if (playerRoot == null && actionScript != null)
        {
            playerRoot = actionScript.transform;
        }

        animator ??= GetComponent<Animator>();
        navMeshAgent ??= GetComponent<NavMeshAgent>();
    }

    private Camera ResolveSourceCamera()
    {
        if (IsUsableCamera(sourceCamera))
        {
            return sourceCamera;
        }

        // Prefer the player's own active camera first, then fall back to any active camera in scene.
        Camera playerCamera = FindCameraOnTransform(actionScript != null ? actionScript.transform : playerRoot);
        if (IsUsableCamera(playerCamera))
        {
            sourceCamera = playerCamera;
            return sourceCamera;
        }

        if (IsUsableCamera(Camera.main))
        {
            sourceCamera = Camera.main;
            return sourceCamera;
        }

        Camera activeCamera = FindAnyUsableCamera();
        if (activeCamera != null)
        {
            sourceCamera = activeCamera;
            return sourceCamera;
        }

        return null;
    }

    private void RefreshAttackSubscription()
    {
        if (subscribedActionScript == actionScript)
        {
            return;
        }

        // Rebind when the boar finds a different player ActionScript at runtime/editor time.
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

    private void CacheAnimatorData()
    {
        walkingAroundHash = Animator.StringToHash(WalkingAroundParameterName);
        hasWalkingAroundParameter = false;

        if (animator == null)
        {
            return;
        }

        AnimatorControllerParameter[] parameters = animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].type == AnimatorControllerParameterType.Bool &&
                parameters[i].name == WalkingAroundParameterName)
            {
                hasWalkingAroundParameter = true;
                return;
            }
        }
    }

    private void SetWalkingState(bool isWalking)
    {
        if (animator == null || !hasWalkingAroundParameter)
        {
            return;
        }

        animator.SetBool(walkingAroundHash, isWalking);
    }

    private void RotateTowards(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime);
    }

    private Vector3 GetInitialRoamCenter()
    {
        return transform.position;
    }

    private Vector3 ClampToRoamArea(Vector3 worldPosition)
    {
        Vector3 offset = worldPosition - roamCenter;
        offset.y = 0f;

        if (offset.sqrMagnitude > roamRadius * roamRadius)
        {
            offset = offset.normalized * roamRadius;
        }

        return roamCenter + offset;
    }

    private Vector3 GetGroundAlignedPosition(Vector3 worldPosition)
    {
        Terrain terrain = Terrain.activeTerrain;
        if (terrain != null)
        {
            worldPosition.y = terrain.SampleHeight(worldPosition) + terrain.GetPosition().y;
            return worldPosition;
        }

        Ray ray = new Ray(worldPosition + Vector3.up * groundProbeHeight, Vector3.down);
        RaycastHit[] hits = Physics.RaycastAll(
            ray,
            groundProbeDistance + groundProbeHeight,
            groundMask,
            QueryTriggerInteraction.Ignore);

        float bestDistance = float.MaxValue;
        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.transform == null || hit.transform.IsChildOf(transform))
            {
                continue;
            }

            if (hit.distance < bestDistance)
            {
                bestDistance = hit.distance;
                worldPosition.y = hit.point.y;
            }
        }

        return worldPosition;
    }

    private bool CanUseNavMesh()
    {
        return navMeshAgent != null &&
            navMeshAgent.enabled &&
            navMeshAgent.isOnNavMesh;
    }

    private static Camera FindCameraOnTransform(Transform root)
    {
        if (root == null)
        {
            return null;
        }

        Camera[] childCameras = root.GetComponentsInChildren<Camera>(true);
        for (int i = 0; i < childCameras.Length; i++)
        {
            Camera camera = childCameras[i];
            if (IsUsableCamera(camera))
            {
                return camera;
            }
        }

        for (int i = 0; i < childCameras.Length; i++)
        {
            Camera camera = childCameras[i];
            if (camera != null)
            {
                return camera;
            }
        }

        return null;
    }

    private static Camera FindAnyUsableCamera()
    {
        Camera[] cameras = FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < cameras.Length; i++)
        {
            Camera camera = cameras[i];
            if (IsUsableCamera(camera))
            {
                return camera;
            }
        }

        return null;
    }

    private static bool IsUsableCamera(Camera camera)
    {
        return camera != null &&
            camera.isActiveAndEnabled &&
            camera.gameObject.activeInHierarchy;
    }

    private static bool TryGetCombinedBounds(GameObject target, out Bounds bounds)
    {
        if (target == null)
        {
            bounds = default;
            return false;
        }

        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return true;
        }

        Collider[] colliders = target.GetComponentsInChildren<Collider>();
        if (colliders.Length > 0)
        {
            bounds = colliders[0].bounds;
            for (int i = 1; i < colliders.Length; i++)
            {
                bounds.Encapsulate(colliders[i].bounds);
            }

            return true;
        }

        bounds = new Bounds(target.transform.position, Vector3.one);
        return false;
    }

    private void ClampSerializedValues()
    {
        hitsToKill = Mathf.Max(1, hitsToKill);
        attackRange = Mathf.Max(0.5f, attackRange);
        targetBoundsPadding = Mathf.Max(0f, targetBoundsPadding);
        roamRadius = Mathf.Max(1f, roamRadius);
        destinationTolerance = Mathf.Max(0.1f, destinationTolerance);
        moveSpeed = Mathf.Max(0.1f, moveSpeed);
        rotationSpeed = Mathf.Max(0.1f, rotationSpeed);
        navMeshSampleDistance = Mathf.Max(0.5f, navMeshSampleDistance);
        groundProbeHeight = Mathf.Max(0.25f, groundProbeHeight);
        groundProbeDistance = Mathf.Max(1f, groundProbeDistance);
        idleDurationRange.x = Mathf.Max(0f, idleDurationRange.x);
        idleDurationRange.y = Mathf.Max(idleDurationRange.x, idleDurationRange.y);
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
