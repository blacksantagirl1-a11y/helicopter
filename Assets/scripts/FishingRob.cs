using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[DefaultExecutionOrder(60)]
public class FishingRob : MonoBehaviour
{
    private const float TargetZoneCenterNormalized = 0.5f;

    private enum FishingState
    {
        Inactive,
        Starting,
        WaitingForBite,
        BiteReady,
        HookChallenge
    }

    private struct BehaviourRestoreState
    {
        public Behaviour Behaviour;
        public bool WasEnabled;
    }

    private struct HandChildRestoreState
    {
        public GameObject GameObject;
        public bool WasActive;
    }

    [Header("Fishing Area")]
    [SerializeField] private string targetWaterVolumeName = "WaterVolume (6)";
    [SerializeField] private string fishingTriggerObjectName = "FishingTrigger";
    [SerializeField] private Transform fishPointTransform;
    [SerializeField] [Min(1f)] private float fishingStartDistance = 4f;
    [SerializeField] [Range(0f, 90f)] private float facingAngleThreshold = 55f;
    [SerializeField] [Min(0f)] private float shoreOffset = 1.15f;
    [SerializeField] [Min(0.5f)] private float castDistanceIntoWater = 4f;

    [Header("Fishing Timing")]
    [SerializeField] private Vector2 biteDelayRange = new Vector2(2.5f, 5f);
    [SerializeField] [Min(0.5f)] private float biteResponseWindow = 1.8f;
    [SerializeField] [Min(0.15f)] private float castLineTravelDuration = 0.45f;
    [SerializeField] [Min(0f)] private float autoOpenMinigameDelay = 0.45f;

    [Header("Mini Game")]
    [SerializeField] [Range(0.1f, 0.6f)] private float targetZoneWidth = 0.22f;
    [SerializeField] [Min(0.2f)] private float fishTravelSeconds = 1.2f;
    [SerializeField] [Min(0f)] private float fishSpinSpeed = 120f;

    [Header("Fishing Camera")]
    [SerializeField] private bool lockCameraDuringFishing = false;
    [SerializeField] [Min(0f)] private float cameraFocusHeight = 0.38f;
    [SerializeField] [Range(1f, 20f)] private float cameraAimLerpSpeed = 8f;

    [Header("Rod Attach")]
    [SerializeField] private Vector3 rodLocalPosition = new Vector3(0.05f, 0.03f, 0.02f);
    [SerializeField] private Vector3 rodLocalEulerAngles = new Vector3(6f, 92f, 94f);
    [SerializeField] [Min(0.2f)] private float preferredRodLength = 1.15f;
    [SerializeField] [Min(0.01f)] private float rodScaleMultiplier = 1f;

    [Header("Runtime Resources")]
    [SerializeField] private string fishPreviewResourcePath = "Fishing/FishPreview";
    [SerializeField] private string fishItemResourcePath = "Inventory/Fish";

    private readonly List<BehaviourRestoreState> disabledControls = new List<BehaviourRestoreState>();
    private readonly List<HandChildRestoreState> hiddenHandChildren = new List<HandChildRestoreState>();

    private PlayerMovement playerMovement;
    private ActionScript actionScript;
    private PlayerUI playerUI;
    private PlayerInventory playerInventory;
    private Stamina stamina;
    private InventoryUIController inventoryUI;
    private Jump jump;
    private Camera mainCamera;
    private PlayerLook playerLook;
    private Zoom zoom;
    private PickUpScript pickUpScript;
    private CuttingTreeSystem cuttingTreeSystem;
    private Rigidbody playerRigidbody;
    private CapsuleCollider playerCollider;
    private Animator animator;
    private Transform leftHand;
    private Transform rightHand;
    private Terrain terrain;

    private Renderer targetWaterRenderer;
    private readonly List<Collider> fishingInteractionTriggers = new List<Collider>();

    private GameObject fishingRodPrefab;
    private GameObject fishPreviewPrefab;
    private InventoryItemDefinition fishItemDefinition;

    private GameObject rodInstance;
    private Transform robPointTransform;
    private LineRenderer lineRenderer;
    private Material lineMaterial;

    private Canvas canvas;
    private RectTransform canvasRect;
    private RectTransform fishingUiRoot;
    private RectTransform minigamePanel;
    private RectTransform trackRect;
    private RectTransform targetRect;
    private RectTransform fishRect;
    private RawImage fishPreviewImage;
    private TextMeshProUGUI fishingHintLabel;
    private TextMeshProUGUI statusLabel;

    private GameObject fishPreviewStage;
    private GameObject fishPreviewInstance;
    private Camera fishPreviewCamera;
    private RenderTexture fishPreviewTexture;

    private FishingState currentState = FishingState.Inactive;
    private Coroutine fishingRoutine;
    private Coroutine biteRoutine;
    private bool controlsLocked;
    private bool hasFishingCandidate;
    private Vector3 candidateFishingSpot;
    private Vector3 candidateCastPoint;
    private Vector3 fishingSpot;
    private Vector3 castPoint;
    private Vector3 currentHookPoint;
    private float biteDeadline;
    private float fishMotionOffset;
    private string transientStatus = string.Empty;
    private bool missingRobPointWarningLogged;
    private bool missingFishPointWarningLogged;
    private bool isInsideFishingTrigger;

    public bool ShouldOverrideDefaultInteraction => currentState != FishingState.Inactive || hasFishingCandidate;

    public string CurrentPrompt =>
        currentState == FishingState.Inactive && hasFishingCandidate
            ? "Nhan E de cau ca"
            : string.Empty;

    public void RefreshInteractionAvailability()
    {
        if (currentState == FishingState.Inactive)
        {
            RefreshFishingCandidate();
        }
    }

    private void Awake()
    {
        InitializeFishingRuntime();
    }

    private void OnEnable()
    {
        InitializeFishingRuntime();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsFishingInteractionTrigger(other))
        {
            isInsideFishingTrigger = true;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (IsFishingInteractionTrigger(other))
        {
            isInsideFishingTrigger = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsFishingInteractionTrigger(other))
        {
            RefreshFishingTriggerState();
        }
    }

    private void Update()
    {
        if (mainCamera == null || playerUI == null || actionScript == null || pickUpScript == null)
        {
            TryAssignReferences();
        }

        if (fishingRodPrefab == null || fishPreviewPrefab == null || fishItemDefinition == null)
        {
            TryLoadRuntimeResources();
        }

        RefreshFishingCandidate();
        UpdatePromptIfNeeded();
        UpdateHookPoint();
        UpdateLineRenderer();
        UpdateFishingCamera();
        UpdateMinigameUi();
        UpdateFishPreviewStage();

        if (currentState != FishingState.Inactive && Input.GetKeyDown(KeyCode.Q))
        {
            ExitFishingMode();
            return;
        }

        if ((pickUpScript == null || !pickUpScript.enabled) && Input.GetKeyDown(KeyCode.E))
        {
            TryConsumeInteractInput();
        }
    }

    private void OnDisable()
    {
        CleanupFishingSession();
    }

    private void OnDestroy()
    {
        CleanupFishingSession();
        ReleasePreviewResources();

        if (lineMaterial != null)
        {
            Destroy(lineMaterial);
        }
    }

    public bool TryConsumeInteractInput()
    {
        switch (currentState)
        {
            case FishingState.Inactive:
                RefreshInteractionAvailability();
                if (!hasFishingCandidate || IsInventoryOpen())
                {
                    return false;
                }

                BeginFishingMode();
                return true;

            case FishingState.BiteReady:
                BeginHookChallenge();
                return true;

            case FishingState.HookChallenge:
                ResolveHookAttempt();
                return true;

            default:
                return currentState != FishingState.Inactive;
        }
    }

    private void BeginFishingMode()
    {
        if (fishingRoutine != null)
        {
            StopCoroutine(fishingRoutine);
        }

        fishingRoutine = StartCoroutine(BeginFishingModeRoutine());
    }

    private IEnumerator BeginFishingModeRoutine()
    {
        currentState = FishingState.Starting;
        fishingSpot = candidateFishingSpot;
        castPoint = candidateCastPoint;
        transientStatus = string.Empty;
        missingFishPointWarningLogged = false;

        SnapPlayerToFishingSpot();
        HideCurrentHandItems();
        AttachRodToHand();
        EnsureLineRenderer();
        currentHookPoint = GetRodTipPosition();
        UpdateLineRenderer();

        LockGameplayControls();
        TryRefreshCastPointFromSpawnPoint();
        currentHookPoint = castPoint;
        if (lineRenderer != null)
        {
            lineRenderer.enabled = true;
            UpdateLineRenderer();
        }

        EnterHookChallengeState();
        fishingRoutine = null;
        yield break;
    }

    private IEnumerator AnimateCastLine()
    {
        EnsureLineRenderer();
        if (lineRenderer == null)
        {
            yield break;
        }

        lineRenderer.enabled = true;

        Vector3 startPoint = GetRodTipPosition();
        float elapsed = 0f;
        float duration = Mathf.Max(0.15f, castLineTravelDuration);
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / duration);
            currentHookPoint = Vector3.Lerp(startPoint, castPoint, normalizedTime);
            UpdateLineRenderer();
            yield return null;
        }

        currentHookPoint = castPoint;
        UpdateLineRenderer();
    }

    private void ScheduleNextBite()
    {
        if (biteRoutine != null)
        {
            StopCoroutine(biteRoutine);
        }

        biteRoutine = StartCoroutine(BiteRoutine());
    }

    private IEnumerator BiteRoutine()
    {
        currentState = FishingState.WaitingForBite;
        transientStatus = "Dang doi ca can cau...";
        float waitDelay = Random.Range(
            Mathf.Min(biteDelayRange.x, biteDelayRange.y),
            Mathf.Max(biteDelayRange.x, biteDelayRange.y));

        yield return new WaitForSeconds(waitDelay);

        currentState = FishingState.BiteReady;
        transientStatus = "Nhan E de vao mini game.";
        biteDeadline = Time.time + Mathf.Max(0.5f, biteResponseWindow);
        float autoOpenTime = Time.time + Mathf.Min(autoOpenMinigameDelay, biteResponseWindow);

        while (currentState == FishingState.BiteReady && Time.time < biteDeadline)
        {
            if (Time.time >= autoOpenTime)
            {
                EnterHookChallengeState();
                biteRoutine = null;
                yield break;
            }

            yield return null;
        }

        if (currentState == FishingState.BiteReady)
        {
            transientStatus = "Ca da bo di.";
            biteRoutine = null;
            ScheduleNextBite();
            yield break;
        }

        biteRoutine = null;
    }

    private void BeginHookChallenge()
    {
        if (biteRoutine != null)
        {
            StopCoroutine(biteRoutine);
            biteRoutine = null;
        }

        EnterHookChallengeState();
    }

    private void EnterHookChallengeState()
    {
        currentState = FishingState.HookChallenge;
        fishMotionOffset = Random.Range(0f, 100f);
        transientStatus = "Nhan E khi ca vao o vuong giua.";

        EnsureUi();
        if (minigamePanel != null)
        {
            minigamePanel.gameObject.SetActive(true);
        }
    }

    private void ResolveHookAttempt()
    {
        bool success = IsFishInsideTargetZone();
        if (success)
        {
            TryStoreFish();
            transientStatus = "Da cau duoc ca!";
        }
        else
        {
            transientStatus = "Ca thoat roi. Tiep tuc cho ca can.";
        }

        if (minigamePanel != null)
        {
            minigamePanel.gameObject.SetActive(false);
        }

        currentState = FishingState.WaitingForBite;
        ScheduleNextBite();
    }

    private void TryStoreFish()
    {
        if (fishItemDefinition == null)
        {
            return;
        }

        if (playerInventory != null)
        {
            playerInventory.TryAddItem(fishItemDefinition, 1, out _);
        }

        stamina?.ConsumeFishingCatchStamina();
    }

    private void ExitFishingMode()
    {
        transientStatus = string.Empty;
        CleanupFishingSession();
    }

    private void CleanupFishingSession()
    {
        if (biteRoutine != null)
        {
            StopCoroutine(biteRoutine);
            biteRoutine = null;
        }

        if (fishingRoutine != null)
        {
            StopCoroutine(fishingRoutine);
            fishingRoutine = null;
        }

        currentState = FishingState.Inactive;
        RestoreGameplayControls();
        RestoreHandItems();
        DestroyFishingRod();

        if (minigamePanel != null)
        {
            minigamePanel.gameObject.SetActive(false);
        }

        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
        }

        if (playerUI != null)
        {
            playerUI.UpdatePrompt(string.Empty);
        }

        if (fishingHintLabel != null)
        {
            fishingHintLabel.gameObject.SetActive(false);
        }
    }

    private void LockGameplayControls()
    {
        if (controlsLocked)
        {
            return;
        }

        controlsLocked = true;
        disabledControls.Clear();

        CacheAndDisableBehaviour(playerMovement);
        CacheAndDisableBehaviour(jump);
        if (lockCameraDuringFishing)
        {
            CacheAndDisableBehaviour(playerLook);
        }
        CacheAndDisableBehaviour(zoom);
        CacheAndDisableBehaviour(pickUpScript);
        CacheAndDisableBehaviour(actionScript);
        CacheAndDisableBehaviour(cuttingTreeSystem);
        CacheAndDisableBehaviour(inventoryUI);

        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
        }
    }

    private void RestoreGameplayControls()
    {
        if (!controlsLocked)
        {
            return;
        }

        for (int i = 0; i < disabledControls.Count; i++)
        {
            BehaviourRestoreState restoreState = disabledControls[i];
            if (restoreState.Behaviour != null)
            {
                restoreState.Behaviour.enabled = restoreState.WasEnabled;
            }
        }

        disabledControls.Clear();
        controlsLocked = false;
    }

    private void CacheAndDisableBehaviour(Behaviour behaviour)
    {
        if (behaviour == null || behaviour == this)
        {
            return;
        }

        disabledControls.Add(new BehaviourRestoreState
        {
            Behaviour = behaviour,
            WasEnabled = behaviour.enabled
        });
        behaviour.enabled = false;
    }

    private void HideCurrentHandItems()
    {
        hiddenHandChildren.Clear();
        HideHandChildren(leftHand);

        if (rightHand != leftHand)
        {
            HideHandChildren(rightHand);
        }
    }

    private void RestoreHandItems()
    {
        for (int i = 0; i < hiddenHandChildren.Count; i++)
        {
            HandChildRestoreState restoreState = hiddenHandChildren[i];
            if (restoreState.GameObject != null)
            {
                restoreState.GameObject.SetActive(restoreState.WasActive);
            }
        }

        hiddenHandChildren.Clear();
    }

    private void AttachRodToHand()
    {
        Transform rodHand = GetRodAttachHand();
        if (fishingRodPrefab == null || rodHand == null)
        {
            return;
        }

        DestroyFishingRod();
        rodInstance = Instantiate(fishingRodPrefab, rodHand);
        Vector3 localPosition = rodLocalPosition;
        Vector3 localEulerAngles = rodLocalEulerAngles;
        if (leftHand != null && rodHand == leftHand)
        {
            localPosition.x *= -1f;
            localEulerAngles.y *= -1f;
            localEulerAngles.z *= -1f;
        }

        rodInstance.transform.localPosition = localPosition;
        rodInstance.transform.localRotation = Quaternion.Euler(localEulerAngles);
        rodInstance.transform.localScale = Vector3.one * GetRodScaleFactor() * rodScaleMultiplier;
        SetLayerRecursively(rodInstance.transform, gameObject.layer);
        missingRobPointWarningLogged = false;
        robPointTransform = ResolveRobPointTransform();
    }

    private void DestroyFishingRod()
    {
        if (rodInstance != null)
        {
            Destroy(rodInstance);
            rodInstance = null;
        }

        robPointTransform = null;
    }

    private float GetRodScaleFactor()
    {
        if (fishingRodPrefab == null)
        {
            return 1f;
        }

        Renderer prefabRenderer = fishingRodPrefab.GetComponentInChildren<Renderer>(true);
        if (prefabRenderer == null)
        {
            return 1f;
        }

        Vector3 size = prefabRenderer.bounds.size;
        float maxDimension = Mathf.Max(size.x, size.y, size.z);
        if (maxDimension <= 0.001f)
        {
            return 1f;
        }

        return preferredRodLength / maxDimension;
    }

    private void EnsureLineRenderer()
    {
        if (lineRenderer != null)
        {
            return;
        }

        GameObject lineObject = new GameObject("FishingLine");
        lineObject.transform.SetParent(transform, false);
        lineRenderer = lineObject.AddComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;
        lineRenderer.startWidth = 0.015f;
        lineRenderer.endWidth = 0.01f;
        lineRenderer.numCapVertices = 4;
        lineRenderer.enabled = false;

        Shader lineShader = Shader.Find("Sprites/Default");
        if (lineShader != null)
        {
            lineMaterial = new Material(lineShader);
            lineMaterial.color = new Color(0.95f, 0.95f, 0.95f, 0.95f);
            lineRenderer.material = lineMaterial;
        }
    }

    private void UpdateLineRenderer()
    {
        if (lineRenderer == null || !lineRenderer.enabled)
        {
            return;
        }

        if (!TryGetLineStartPosition(out Vector3 lineStartPosition) ||
            !TryGetLineEndPosition(out Vector3 lineEndPosition))
        {
            lineRenderer.enabled = false;
            return;
        }

        lineRenderer.SetPosition(0, lineStartPosition);
        lineRenderer.SetPosition(1, lineEndPosition);
    }

    private Vector3 GetRodTipPosition()
    {
        if (robPointTransform != null)
        {
            return robPointTransform.position;
        }

        if (rodInstance != null)
        {
            Renderer[] rodRenderers = rodInstance.GetComponentsInChildren<Renderer>();
            Bounds? bounds = GetCombinedBounds(rodRenderers);
            if (bounds.HasValue)
            {
                Transform rodHand = GetRodAttachHand();
                Vector3 handPosition = rodHand != null ? rodHand.position : rodInstance.transform.position;
                Vector3 roughTip = bounds.Value.center +
                                   rodInstance.transform.forward * bounds.Value.extents.magnitude;
                Vector3 directionFromHand = roughTip - handPosition;
                if (directionFromHand.sqrMagnitude > 0.001f)
                {
                    return bounds.Value.center +
                           directionFromHand.normalized * bounds.Value.extents.magnitude * 1.05f;
                }

                return roughTip;
            }

            return rodInstance.transform.position + rodInstance.transform.forward * 0.75f;
        }

        Transform fallbackHand = GetRodAttachHand();
        if (fallbackHand != null)
        {
            return fallbackHand.position + fallbackHand.forward * 0.45f;
        }

        return transform.position + transform.forward * 0.75f;
    }

    private bool TryGetLineStartPosition(out Vector3 lineStartPosition)
    {
        if (robPointTransform == null)
        {
            lineStartPosition = Vector3.zero;
            WarnMissingRobPointOnce();
            return false;
        }

        lineStartPosition = robPointTransform.position;
        return true;
    }

    private bool TryGetLineEndPosition(out Vector3 lineEndPosition)
    {
        if (fishPointTransform == null)
        {
            lineEndPosition = Vector3.zero;
            WarnMissingFishPointOnce();
            return false;
        }

        lineEndPosition = fishPointTransform.position;
        return true;
    }

    private void HideHandChildren(Transform hand)
    {
        if (hand == null)
        {
            return;
        }

        for (int i = 0; i < hand.childCount; i++)
        {
            Transform child = hand.GetChild(i);
            if (child == null)
            {
                continue;
            }

            hiddenHandChildren.Add(new HandChildRestoreState
            {
                GameObject = child.gameObject,
                WasActive = child.gameObject.activeSelf
            });
            child.gameObject.SetActive(false);
        }
    }

    private Transform GetRodAttachHand()
    {
        return leftHand != null ? leftHand : rightHand;
    }

    private Transform ResolveRobPointTransform()
    {
        if (rodInstance == null)
        {
            return null;
        }

        FishingRodReferences rodReferences = rodInstance.GetComponentInChildren<FishingRodReferences>(true);
        return rodReferences != null ? rodReferences.RobPoint : null;
    }

    private void WarnMissingRobPointOnce()
    {
        if (missingRobPointWarningLogged)
        {
            return;
        }

        missingRobPointWarningLogged = true;
        string rodName = rodInstance != null ? rodInstance.name : "FishingRodPrefab";
        Debug.LogWarning(
            $"FishingRob is missing a RobPoint reference on rod '{rodName}'. Assign FishingRodReferences.robPoint on the fishing rod prefab.",
            this);
    }

    private void WarnMissingFishPointOnce()
    {
        if (missingFishPointWarningLogged)
        {
            return;
        }

        missingFishPointWarningLogged = true;
        Debug.LogWarning(
            "FishingRob is missing FishPoint. Assign FishingRob.fishPointTransform in the Inspector to render the fishing line.",
            this);
    }

    private void RefreshFishingCandidate()
    {
        if (currentState != FishingState.Inactive)
        {
            hasFishingCandidate = false;
            candidateFishingSpot = Vector3.zero;
            candidateCastPoint = Vector3.zero;
            return;
        }

        if (!IsPlayerInsideFishingTrigger())
        {
            hasFishingCandidate = false;
            candidateFishingSpot = Vector3.zero;
            candidateCastPoint = Vector3.zero;
            return;
        }

        hasFishingCandidate = TryCalculateFishingPoints(out candidateFishingSpot, out candidateCastPoint);
        if (!hasFishingCandidate || mainCamera == null)
        {
            return;
        }

        Vector3 toCastPoint = candidateCastPoint - mainCamera.transform.position;
        toCastPoint.y = 0f;
        Vector3 forward = mainCamera.transform.forward;
        forward.y = 0f;

        if (toCastPoint.sqrMagnitude <= 0.001f || forward.sqrMagnitude <= 0.001f)
        {
            return;
        }

        float angle = Vector3.Angle(forward.normalized, toCastPoint.normalized);
        hasFishingCandidate = angle <= facingAngleThreshold;
    }

    private void TryRefreshCastPointFromSpawnPoint()
    {
        if (!TryResolveWaterBounds(out Bounds waterBounds))
        {
            return;
        }

        if (TryCalculateCastPointFromSpawnPoint(waterBounds, out Vector3 spawnAlignedCastPoint))
        {
            castPoint = spawnAlignedCastPoint;
        }
    }

    private bool TryCalculateFishingPoints(out Vector3 spot, out Vector3 hookPoint)
    {
        spot = Vector3.zero;
        hookPoint = Vector3.zero;

        if (!TryResolveWaterBounds(out Bounds waterBounds))
        {
            return false;
        }

        Vector3 playerPosition = transform.position;
        Vector3 playerFlat = new Vector3(playerPosition.x, waterBounds.center.y, playerPosition.z);
        Vector3 nearestPoint = waterBounds.ClosestPoint(playerFlat);

        Vector3 outward = new Vector3(
            nearestPoint.x - waterBounds.center.x,
            0f,
            nearestPoint.z - waterBounds.center.z);

        if (outward.sqrMagnitude <= 0.001f)
        {
            outward = new Vector3(
                playerPosition.x - waterBounds.center.x,
                0f,
                playerPosition.z - waterBounds.center.z);
        }

        if (outward.sqrMagnitude <= 0.001f)
        {
            outward = transform.forward;
            outward.y = 0f;
        }

        outward.Normalize();

        spot = nearestPoint + outward * shoreOffset;
        float terrainHeight = playerPosition.y;
        if (terrain != null)
        {
            terrainHeight = terrain.SampleHeight(spot) + terrain.GetPosition().y;
        }

        spot.y = terrainHeight;
        hookPoint = nearestPoint - outward * castDistanceIntoWater;
        hookPoint.y = waterBounds.max.y;

        Vector2 playerFlat2D = new Vector2(playerPosition.x, playerPosition.z);
        Vector2 spotFlat2D = new Vector2(spot.x, spot.z);
        return Vector2.Distance(playerFlat2D, spotFlat2D) <= fishingStartDistance;
    }

    private bool TryCalculateCastPointFromSpawnPoint(Bounds waterBounds, out Vector3 hookPoint)
    {
        hookPoint = Vector3.zero;
        Vector3 spawnWorldPosition = GetRodTipPosition();
        Vector3 castDirection = Vector3.ProjectOnPlane(robPointTransform != null ? robPointTransform.forward : transform.forward, Vector3.up);
        if (castDirection.sqrMagnitude <= 0.001f)
        {
            return false;
        }

        castDirection.Normalize();

        Vector2 origin = new Vector2(spawnWorldPosition.x, spawnWorldPosition.z);
        Vector2 direction = new Vector2(castDirection.x, castDirection.z);
        if (direction.sqrMagnitude <= 0.001f)
        {
            return false;
        }

        direction.Normalize();
        Bounds flatWaterBounds = waterBounds;
        float tEnter = 0f;
        float tExit = 0f;
        if (!TryIntersectRayWithBounds2D(origin, direction, flatWaterBounds, out tEnter, out tExit))
        {
            return false;
        }

        float entryDistance = Mathf.Max(0f, tEnter);
        float exitDistance = Mathf.Max(entryDistance, tExit);
        float maxDistanceInsideWater = Mathf.Max(entryDistance, exitDistance - 0.1f);
        float targetDistance = Mathf.Clamp(entryDistance + castDistanceIntoWater, entryDistance, maxDistanceInsideWater);

        Vector2 hitPoint2D = origin + direction * targetDistance;
        hookPoint = new Vector3(hitPoint2D.x, waterBounds.max.y, hitPoint2D.y);
        hookPoint.x = Mathf.Clamp(hookPoint.x, waterBounds.min.x, waterBounds.max.x);
        hookPoint.z = Mathf.Clamp(hookPoint.z, waterBounds.min.z, waterBounds.max.z);
        return true;
    }

    private bool TryIntersectRayWithBounds2D(Vector2 origin, Vector2 direction, Bounds bounds, out float tEnter, out float tExit)
    {
        tEnter = float.NegativeInfinity;
        tExit = float.PositiveInfinity;

        if (!ClipRayToAxis(origin.x, direction.x, bounds.min.x, bounds.max.x, ref tEnter, ref tExit))
        {
            return false;
        }

        if (!ClipRayToAxis(origin.y, direction.y, bounds.min.z, bounds.max.z, ref tEnter, ref tExit))
        {
            return false;
        }

        return tExit >= Mathf.Max(0f, tEnter);
    }

    private bool ClipRayToAxis(float origin, float direction, float min, float max, ref float tEnter, ref float tExit)
    {
        if (Mathf.Abs(direction) <= 0.0001f)
        {
            return origin >= min && origin <= max;
        }

        float t0 = (min - origin) / direction;
        float t1 = (max - origin) / direction;
        if (t0 > t1)
        {
            (t0, t1) = (t1, t0);
        }

        tEnter = Mathf.Max(tEnter, t0);
        tExit = Mathf.Min(tExit, t1);
        return tExit >= tEnter;
    }

    private bool TryResolveWaterBounds(out Bounds waterBounds)
    {
        waterBounds = default;
        if (targetWaterRenderer == null)
        {
            ResolveTargetWater();
        }

        if (targetWaterRenderer == null)
        {
            return false;
        }

        waterBounds = targetWaterRenderer.bounds;
        return waterBounds.size.sqrMagnitude > 0.001f;
    }

    private void ResolveTargetWater()
    {
        if (targetWaterRenderer != null)
        {
            return;
        }

        GameObject waterObject = GameObject.Find(targetWaterVolumeName);
        if (waterObject == null)
        {
            Renderer[] renderers = FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer candidateRenderer = renderers[i];
                if (candidateRenderer == null)
                {
                    continue;
                }

                if (candidateRenderer.gameObject.name == targetWaterVolumeName)
                {
                    waterObject = candidateRenderer.gameObject;
                    break;
                }
            }
        }

        if (waterObject == null)
        {
            return;
        }

        targetWaterRenderer = waterObject.GetComponent<Renderer>();
        if (targetWaterRenderer == null)
        {
            targetWaterRenderer = waterObject.GetComponentInChildren<Renderer>();
        }
    }

    private void SnapPlayerToFishingSpot()
    {
        Vector3 targetPosition = new Vector3(fishingSpot.x, transform.position.y, fishingSpot.z);
        if (playerRigidbody != null)
        {
            playerRigidbody.position = targetPosition;
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
        }

        transform.position = targetPosition;

        Vector3 lookDirection = castPoint - transform.position;
        lookDirection.y = 0f;
        if (lookDirection.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
        }
    }

    private void UpdateHookPoint()
    {
        if (currentState == FishingState.Inactive || currentState == FishingState.Starting)
        {
            return;
        }

        currentHookPoint = castPoint;
    }

    private void UpdateFishingCamera()
    {
        if (!lockCameraDuringFishing)
        {
            return;
        }

        if (!controlsLocked || mainCamera == null || playerLook == null)
        {
            return;
        }

        if (currentState != FishingState.WaitingForBite &&
            currentState != FishingState.BiteReady &&
            currentState != FishingState.HookChallenge)
        {
            return;
        }

        Vector3 focusPoint = currentHookPoint;
        if (focusPoint == Vector3.zero)
        {
            focusPoint = castPoint;
        }

        focusPoint += Vector3.up * cameraFocusHeight;
        Vector3 lookDirection = focusPoint - mainCamera.transform.position;
        if (lookDirection.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Quaternion desiredRotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
        mainCamera.transform.rotation = Quaternion.Slerp(
            mainCamera.transform.rotation,
            desiredRotation,
            Time.deltaTime * cameraAimLerpSpeed);

        playerLook.ForceLookTowards(mainCamera.transform.forward);
    }

    private void UpdatePromptIfNeeded()
    {
        UpdateFishingHintUi();

        if (playerUI == null || pickUpScript != null && pickUpScript.enabled)
        {
            return;
        }

        playerUI.UpdatePrompt(currentState == FishingState.Inactive ? CurrentPrompt : string.Empty);
    }

    private void UpdateFishingHintUi()
    {
        if (fishingHintLabel == null)
        {
            return;
        }

        string hintText = currentState switch
        {
            FishingState.WaitingForBite => "Q de thoat",
            FishingState.BiteReady => "Ca can! E de vao mini game   |   Q de thoat",
            FishingState.HookChallenge => "Q de thoat",
            _ => string.Empty
        };

        fishingHintLabel.text = hintText;
        fishingHintLabel.gameObject.SetActive(!string.IsNullOrWhiteSpace(hintText));
    }

    private void UpdateMinigameUi()
    {
        if (minigamePanel == null || trackRect == null || targetRect == null || fishRect == null)
        {
            return;
        }

        bool showMinigame = currentState == FishingState.HookChallenge;
        minigamePanel.gameObject.SetActive(showMinigame);
        if (!showMinigame)
        {
            return;
        }

        if (statusLabel != null)
        {
            statusLabel.text = string.IsNullOrWhiteSpace(transientStatus)
                ? "Nhan E khi ca vao o vuong giua."
                : transientStatus;
        }

        float trackWidth = trackRect.rect.width;
        float targetWidthPixels = trackWidth * Mathf.Clamp01(targetZoneWidth);
        targetRect.sizeDelta = new Vector2(targetWidthPixels, targetRect.sizeDelta.y);
        targetRect.anchoredPosition = Vector2.zero;

        float fishNormalized = GetFishPositionNormalized();
        float fishWidth = fishRect.rect.width;
        float fishPosition = Mathf.Lerp(
            -trackWidth * 0.5f + fishWidth * 0.5f,
            trackWidth * 0.5f - fishWidth * 0.5f,
            fishNormalized);
        fishRect.anchoredPosition = new Vector2(fishPosition, 0f);
        fishRect.localScale = new Vector3(IsFishMovingRight() ? -1f : 1f, 1f, 1f);
        fishRect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 7f) * 8f);

        Image targetImage = targetRect.GetComponent<Image>();
        if (targetImage != null)
        {
            bool fishInsideZone = IsFishInsideTargetZone();
            targetImage.color = fishInsideZone
                ? new Color(1f, 0.98f, 0.78f, 0.4f)
                : new Color(1f, 1f, 1f, 0.16f);
        }
    }

    private void UpdateFishPreviewStage()
    {
        if (currentState != FishingState.HookChallenge || fishPreviewInstance == null)
        {
            return;
        }

        fishPreviewInstance.transform.Rotate(Vector3.up, fishSpinSpeed * Time.deltaTime, Space.World);
        if (fishPreviewCamera != null)
        {
            fishPreviewCamera.Render();
        }
    }

    private float GetFishPositionNormalized()
    {
        float duration = Mathf.Max(0.2f, fishTravelSeconds);
        return Mathf.PingPong((Time.time + fishMotionOffset) / duration, 1f);
    }

    private bool IsFishMovingRight()
    {
        float duration = Mathf.Max(0.2f, fishTravelSeconds);
        return Mathf.Repeat((Time.time + fishMotionOffset) / duration, 2f) < 1f;
    }

    private bool IsFishInsideTargetZone()
    {
        float fishPosition = GetFishPositionNormalized();
        float halfZone = targetZoneWidth * 0.5f;
        return fishPosition >= TargetZoneCenterNormalized - halfZone &&
               fishPosition <= TargetZoneCenterNormalized + halfZone;
    }

    private void EnsureUi()
    {
        if (canvas != null && canvasRect != null && fishingUiRoot != null)
        {
            return;
        }

        if (playerUI != null && playerUI.PickUpText != null)
        {
            canvas = playerUI.PickUpText.canvas;
        }

        if (canvas == null)
        {
            canvas = FindFirstObjectByType<Canvas>();
        }

        if (canvas == null)
        {
            return;
        }

        canvasRect = canvas.GetComponent<RectTransform>();

        Transform existingRoot = canvas.transform.Find("FishingUIRoot");
        if (existingRoot != null)
        {
            fishingUiRoot = existingRoot as RectTransform;
            fishingHintLabel = existingRoot.Find("FishingHintLabel")?.GetComponent<TextMeshProUGUI>();
            minigamePanel = existingRoot.Find("MiniGamePanel") as RectTransform;
            if (minigamePanel != null)
            {
                statusLabel = minigamePanel.Find("StatusLabel")?.GetComponent<TextMeshProUGUI>();
                trackRect = minigamePanel.Find("Track") as RectTransform;
                if (trackRect != null)
                {
                    targetRect = trackRect.Find("TargetZone") as RectTransform;
                    fishRect = trackRect.Find("FishIcon") as RectTransform;
                    if (fishRect != null)
                    {
                        fishPreviewImage = fishRect.GetComponent<RawImage>();
                    }
                }
            }

            return;
        }

        fishingUiRoot = CreateRect("FishingUIRoot", canvas.transform);
        StretchToParent(fishingUiRoot);

        fishingHintLabel = CreateText("FishingHintLabel", fishingUiRoot);
        fishingHintLabel.alignment = TextAlignmentOptions.TopRight;
        fishingHintLabel.fontSize = 36f;
        fishingHintLabel.color = Color.white;
        fishingHintLabel.gameObject.SetActive(false);
        RectTransform hintRect = fishingHintLabel.rectTransform;
        hintRect.anchorMin = new Vector2(1f, 1f);
        hintRect.anchorMax = new Vector2(1f, 1f);
        hintRect.pivot = new Vector2(1f, 1f);
        hintRect.anchoredPosition = new Vector2(-28f, -22f);
        hintRect.sizeDelta = new Vector2(320f, 80f);

        minigamePanel = CreateRect("MiniGamePanel", fishingUiRoot);
        Image panelBackground = minigamePanel.gameObject.AddComponent<Image>();
        panelBackground.color = new Color(0f, 0f, 0f, 0f);
        panelBackground.raycastTarget = false;
        minigamePanel.anchorMin = new Vector2(0.5f, 0f);
        minigamePanel.anchorMax = new Vector2(0.5f, 0f);
        minigamePanel.pivot = new Vector2(0.5f, 0f);
        minigamePanel.anchoredPosition = new Vector2(0f, 88f);
        minigamePanel.sizeDelta = new Vector2(840f, 236f);
        minigamePanel.gameObject.SetActive(false);

        statusLabel = CreateText("StatusLabel", minigamePanel);
        statusLabel.alignment = TextAlignmentOptions.Center;
        statusLabel.fontSize = 36f;
        statusLabel.color = Color.white;
        RectTransform statusRect = statusLabel.rectTransform;
        statusRect.anchorMin = new Vector2(0f, 1f);
        statusRect.anchorMax = new Vector2(1f, 1f);
        statusRect.pivot = new Vector2(0.5f, 1f);
        statusRect.anchoredPosition = new Vector2(0f, -4f);
        statusRect.sizeDelta = new Vector2(-20f, 48f);

        trackRect = CreateRect("Track", minigamePanel);
        Image trackImage = trackRect.gameObject.AddComponent<Image>();
        trackImage.color = new Color(0.04f, 0.12f, 0.22f, 0.84f);
        trackImage.raycastTarget = false;
        Outline trackOutline = trackRect.gameObject.AddComponent<Outline>();
        trackOutline.effectColor = new Color(0f, 0f, 0f, 0.85f);
        trackOutline.effectDistance = new Vector2(2.5f, 2.5f);
        trackRect.anchorMin = new Vector2(0.5f, 0f);
        trackRect.anchorMax = new Vector2(0.5f, 0f);
        trackRect.pivot = new Vector2(0.5f, 0.5f);
        trackRect.anchoredPosition = new Vector2(0f, 30f);
        trackRect.sizeDelta = new Vector2(660f, 108f);
        trackRect.gameObject.AddComponent<RectMask2D>();

        targetRect = CreateRect("TargetZone", trackRect);
        Image targetImage = targetRect.gameObject.AddComponent<Image>();
        targetImage.color = new Color(1f, 1f, 1f, 0.16f);
        targetImage.raycastTarget = false;
        Outline targetOutline = targetRect.gameObject.AddComponent<Outline>();
        targetOutline.effectColor = new Color(1f, 1f, 1f, 0.95f);
        targetOutline.effectDistance = new Vector2(2f, 2f);
        targetRect.anchorMin = new Vector2(0.5f, 0.5f);
        targetRect.anchorMax = new Vector2(0.5f, 0.5f);
        targetRect.pivot = new Vector2(0.5f, 0.5f);
        targetRect.sizeDelta = new Vector2(148f, 108f);

        fishRect = CreateRect("FishIcon", trackRect);
        fishPreviewImage = fishRect.gameObject.AddComponent<RawImage>();
        fishPreviewImage.raycastTarget = false;
        fishPreviewImage.color = Color.white;
        fishRect.anchorMin = new Vector2(0.5f, 0.5f);
        fishRect.anchorMax = new Vector2(0.5f, 0.5f);
        fishRect.pivot = new Vector2(0.5f, 0.5f);
        fishRect.sizeDelta = new Vector2(104f, 104f);

        EnsureFishPreviewResources();
    }

    private void InitializeFishingRuntime()
    {
        TryAssignReferences();
        CacheFishingInteractionTriggers();
        RefreshFishingTriggerState();
        TryLoadRuntimeResources();
        EnsureUi();
    }

    private void EnsureFishPreviewResources()
    {
        if (fishPreviewImage == null || fishPreviewPrefab == null)
        {
            return;
        }

        if (fishPreviewTexture != null && fishPreviewCamera != null && fishPreviewInstance != null)
        {
            fishPreviewImage.texture = fishPreviewTexture;
            return;
        }

        fishPreviewStage = new GameObject("FishingPreviewStage");
        fishPreviewStage.hideFlags = HideFlags.HideAndDontSave;
        fishPreviewStage.transform.position = new Vector3(10000f, 10000f, 10000f);

        fishPreviewInstance = Instantiate(fishPreviewPrefab, fishPreviewStage.transform);
        fishPreviewInstance.transform.localPosition = Vector3.zero;
        fishPreviewInstance.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        SetLayerRecursively(fishPreviewInstance.transform, 0);

        fishPreviewTexture = new RenderTexture(256, 256, 16, RenderTextureFormat.ARGB32);
        fishPreviewTexture.Create();

        fishPreviewCamera = new GameObject("FishingPreviewCamera").AddComponent<Camera>();
        fishPreviewCamera.transform.SetParent(fishPreviewStage.transform, false);
        fishPreviewCamera.clearFlags = CameraClearFlags.SolidColor;
        fishPreviewCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        fishPreviewCamera.farClipPlane = 20f;
        fishPreviewCamera.nearClipPlane = 0.01f;
        fishPreviewCamera.targetTexture = fishPreviewTexture;
        fishPreviewCamera.enabled = false;

        Bounds? fishBounds = GetCombinedBounds(fishPreviewInstance.GetComponentsInChildren<Renderer>());
        if (fishBounds.HasValue)
        {
            Bounds bounds = fishBounds.Value;
            Vector3 focusPoint = bounds.center;
            float distance = Mathf.Max(1.1f, bounds.extents.magnitude * 2.25f);
            fishPreviewCamera.transform.position = focusPoint + new Vector3(0f, bounds.extents.y * 0.2f, -distance);
            fishPreviewCamera.transform.LookAt(focusPoint);
        }
        else
        {
            fishPreviewCamera.transform.localPosition = new Vector3(0f, 0f, -2.5f);
            fishPreviewCamera.transform.LookAt(fishPreviewStage.transform.position);
        }

        fishPreviewCamera.Render();
        fishPreviewImage.texture = fishPreviewTexture;
    }

    private void ReleasePreviewResources()
    {
        if (fishPreviewTexture != null)
        {
            fishPreviewTexture.Release();
            Destroy(fishPreviewTexture);
            fishPreviewTexture = null;
        }

        if (fishPreviewCamera != null)
        {
            Destroy(fishPreviewCamera.gameObject);
            fishPreviewCamera = null;
        }

        if (fishPreviewStage != null)
        {
            Destroy(fishPreviewStage);
            fishPreviewStage = null;
        }

        fishPreviewInstance = null;
    }

    private void TryAssignReferences()
    {
        playerMovement ??= GetComponent<PlayerMovement>();
        actionScript ??= GetComponent<ActionScript>();
        playerUI ??= GetComponent<PlayerUI>();
        playerInventory ??= GetComponent<PlayerInventory>();
        stamina ??= FindFirstObjectByType<Stamina>();
        inventoryUI ??= GetComponent<InventoryUIController>();
        jump ??= GetComponent<Jump>();
        playerRigidbody ??= GetComponent<Rigidbody>();
        playerCollider ??= GetComponent<CapsuleCollider>();
        animator ??= GetComponent<Animator>();
        terrain ??= Terrain.activeTerrain;
        terrain ??= FindFirstObjectByType<Terrain>();

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = GetComponentInChildren<Camera>();
            }
        }

        if (mainCamera != null)
        {
            playerLook ??= mainCamera.GetComponent<PlayerLook>();
            zoom ??= mainCamera.GetComponent<Zoom>();
            pickUpScript ??= mainCamera.GetComponent<PickUpScript>();
            cuttingTreeSystem ??= mainCamera.GetComponent<CuttingTreeSystem>();
        }

        if (leftHand == null && animator != null && animator.isHuman)
        {
            leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
        }

        if (rightHand == null && animator != null && animator.isHuman)
        {
            rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
        }

        if ((leftHand == null || rightHand == null) && animator != null)
        {
            Transform[] allChildren = animator.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < allChildren.Length; i++)
            {
                Transform child = allChildren[i];
                if (child == null)
                {
                    continue;
                }

                if (leftHand == null && child.name.Contains("LeftHand"))
                {
                    leftHand = child;
                }

                if (rightHand == null && child.name.Contains("RightHand"))
                {
                    rightHand = child;
                }

                if (leftHand != null && rightHand != null)
                {
                    break;
                }
            }
        }
    }

    private void TryLoadRuntimeResources()
    {
        if (fishingRodPrefab == null && actionScript != null && actionScript.FishingRodPrefab != null)
        {
            fishingRodPrefab = actionScript.FishingRodPrefab;
        }
        fishPreviewPrefab ??= Resources.Load<GameObject>(fishPreviewResourcePath);
        fishItemDefinition ??= Resources.Load<InventoryItemDefinition>(fishItemResourcePath);

        if (fishPreviewImage != null && fishPreviewImage.texture == null)
        {
            EnsureFishPreviewResources();
        }
    }

    private void CacheFishingInteractionTriggers()
    {
        fishingInteractionTriggers.Clear();
        TryCacheNamedFishingTrigger();
    }

    private bool TryCacheNamedFishingTrigger()
    {
        if (string.IsNullOrWhiteSpace(fishingTriggerObjectName))
        {
            return false;
        }

        GameObject triggerObject = GameObject.Find(fishingTriggerObjectName);
        if (triggerObject == null)
        {
            return false;
        }

        Collider[] colliders = triggerObject.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider collider = colliders[i];
            if (collider == null || !collider.isTrigger)
            {
                continue;
            }

            fishingInteractionTriggers.Add(collider);
        }

        return fishingInteractionTriggers.Count > 0;
    }

    private bool IsFishingInteractionTrigger(Collider collider)
    {
        if (collider == null)
        {
            return false;
        }

        if (fishingInteractionTriggers.Count == 0)
        {
            CacheFishingInteractionTriggers();
        }

        return fishingInteractionTriggers.Contains(collider);
    }

    private void RefreshFishingTriggerState()
    {
        isInsideFishingTrigger = false;

        if (fishingInteractionTriggers.Count == 0)
        {
            CacheFishingInteractionTriggers();
        }

        Bounds playerBounds = playerCollider != null
            ? playerCollider.bounds
            : new Bounds(transform.position, new Vector3(0.5f, 1.8f, 0.5f));

        for (int i = fishingInteractionTriggers.Count - 1; i >= 0; i--)
        {
            Collider trigger = fishingInteractionTriggers[i];
            if (trigger == null)
            {
                fishingInteractionTriggers.RemoveAt(i);
                continue;
            }

            if (!trigger.enabled || !trigger.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (trigger.bounds.Intersects(playerBounds))
            {
                isInsideFishingTrigger = true;
                return;
            }
        }
    }

    private bool IsPlayerInsideFishingTrigger()
    {
        RefreshFishingTriggerState();
        return isInsideFishingTrigger;
    }

    private bool IsInventoryOpen()
    {
        return inventoryUI != null && inventoryUI.IsInventoryOpen;
    }

    private static RectTransform CreateRect(string objectName, Transform parent)
    {
        GameObject gameObject = new GameObject(objectName, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        return gameObject.GetComponent<RectTransform>();
    }

    private static TextMeshProUGUI CreateText(string objectName, Transform parent)
    {
        GameObject gameObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        gameObject.transform.SetParent(parent, false);
        TextMeshProUGUI text = gameObject.GetComponent<TextMeshProUGUI>();
        text.text = string.Empty;
        text.textWrappingMode = TextWrappingModes.Normal;
        return text;
    }

    private static void StretchToParent(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private static Bounds? GetCombinedBounds(Renderer[] renderers)
    {
        if (renderers == null || renderers.Length == 0)
        {
            return null;
        }

        Bounds? combinedBounds = null;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            if (!combinedBounds.HasValue)
            {
                combinedBounds = renderer.bounds;
            }
            else
            {
                Bounds bounds = combinedBounds.Value;
                bounds.Encapsulate(renderer.bounds);
                combinedBounds = bounds;
            }
        }

        return combinedBounds;
    }

    private static void SetLayerRecursively(Transform root, int layer)
    {
        if (root == null)
        {
            return;
        }

        root.gameObject.layer = layer;
        for (int i = 0; i < root.childCount; i++)
        {
            SetLayerRecursively(root.GetChild(i), layer);
        }
    }
}
