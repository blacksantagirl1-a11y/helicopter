using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public sealed class PCVideoInteractable : Interactable
{
    private enum VideoSourcePriority
    {
        VideoClipFirst,
        StreamingAssetsFirst
    }

    private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
    private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    [Header("Prompt")]
    [SerializeField] private string playPrompt = "Nhan E de bat PC";
    [SerializeField] private string replayPrompt = "Nhan E de xem lai video";
    [SerializeField] private bool allowReplay = true;

    [Header("Day Availability")]
    [Tooltip("Ngay bat dau cho phep PC co the tuong tac.")]
    [SerializeField] private DialogueDay availableFromDay = DialogueDay.Day6;
    [Tooltip("Ngay ket thuc cho phep PC co the tuong tac.")]
    [SerializeField] private DialogueDay availableToDay = DialogueDay.Day6;
    [Tooltip("Tat collider khi khong dung ngay de raycast prompt khong bat vao PC nua.")]
    [SerializeField] private bool disableColliderWhenUnavailable = true;

    [Header("PC View")]
    [Tooltip("Diem nhin camera khi nguoi choi dang xem PC. Neu de trong, script se tu tim object ten PCView.")]
    [SerializeField] private Transform pcViewTarget;
    [Tooltip("Ten object se duoc tu tim trong scene lam diem nhin fallback.")]
    [SerializeField] private string pcViewObjectName = "PCView";
    [Tooltip("Thoi gian di chuyen camera toi PCView.")]
    [SerializeField] [Min(0f)] private float moveToPcViewDuration = 0.35f;
    [Tooltip("Thoi gian dua camera tro lai goc nhin gameplay.")]
    [SerializeField] [Min(0f)] private float returnFromPcViewDuration = 0.3f;
    [Tooltip("Tu dong dong inventory neu dang mo truoc khi xem video.")]
    [SerializeField] private bool closeInventoryBeforePlayback = true;

    [Header("Timer UI")]
    [Tooltip("Slider hien thi thoi gian xem PC. Neu de trong, script se tu tim object ten Timer.")]
    [SerializeField] private Slider timerSlider;
    [Tooltip("Ten object Slider duoc tu tim trong scene neu chua gan tay trong Inspector.")]
    [SerializeField] private string timerObjectName = "Timer";
    [Tooltip("So giay de thanh Timer chay tu 0 len 1. Khi day thanh thi camera se tro ve gameplay.")]
    [SerializeField] [Min(0.1f)] private float timerDurationSeconds = 10f;
    [Tooltip("An Timer khi khong dang xem PC de UI gon hon.")]
    [SerializeField] private bool hideTimerWhenInactive = true;

    [Header("Screen")]
    [SerializeField] private MeshRenderer screenRenderer;
    [SerializeField] [Min(0)] private int screenMaterialIndex = 1;
    [SerializeField] private Material screenMaterialTemplate;
    [SerializeField] [Min(256)] private int renderTextureWidth = 1024;
    [SerializeField] [Min(256)] private int renderTextureHeight = 1024;
    [SerializeField] private Color idleScreenColor = Color.black;

    [Header("Video")]
    [Tooltip("Chon nguon video duoc uu tien. Mac dinh la VideoClip truoc de de doi video ngay trong Inspector.")]
    [SerializeField] private VideoSourcePriority videoSourcePriority = VideoSourcePriority.VideoClipFirst;
    [Tooltip("Duong dan toi file video trong StreamingAssets. Chi duoc dung neu uu tien StreamingAssets hoac khi khong co VideoClip.")]
    [SerializeField] private string streamingVideoRelativePath = "Videos/PCScreen.mp4";
    [Tooltip("VideoClip keo tha truc tiep trong Inspector. Mac dinh se duoc uu tien hon file StreamingAssets.")]
    [SerializeField] private VideoClip videoClip;
    [SerializeField] [Min(0.5f)] private float startTimeoutSeconds = 10f;

    private GameObject videoPlayerHost;
    private VideoPlayer videoPlayer;
    private BoxCollider interactionCollider;
    private RenderTexture screenRenderTexture;
    private Material runtimeScreenMaterial;
    private Coroutine waitForPlaybackStartRoutine;
    private Coroutine playbackSequenceRoutine;
    private Coroutine restoreViewRoutine;
    private Coroutine timerRoutine;
    private bool isStartingPlayback;
    private bool isPlaying;
    private bool hasPlayedOnce;
    private bool isViewingPc;
    private bool hasCachedCameraPose;
    private bool hasCachedCursorState;
    private Vector3 cachedCameraLocalPosition;
    private Quaternion cachedCameraLocalRotation;
    private RigidbodyConstraints cachedRigidbodyConstraints;
    private CursorLockMode cachedCursorLockState;
    private bool cachedCursorVisible;
    private readonly Dictionary<Behaviour, bool> cachedControlStates = new Dictionary<Behaviour, bool>();

    private Camera mainCamera;
    private PlayerUI playerUI;
    private InventoryUIController inventoryUIController;
    private PlayerMovement playerMovement;
    private Jump jump;
    private Crouch crouch;
    private PlayerLook playerLook;
    private Zoom zoom;
    private PickUpScript pickUpScript;
    private ActionScript actionScript;
    private CuttingTreeSystem cuttingTreeSystem;
    private Rigidbody playerRigidbody;

    public override bool CanInteract =>
        IsAvailableForCurrentDay() &&
        screenRenderer != null &&
        !isStartingPlayback &&
        !isPlaying &&
        HasAnyVideoSource() &&
        (allowReplay || !hasPlayedOnce);

    public override bool HasPromptText => CanInteract && !string.IsNullOrWhiteSpace(PromptText);
    public override string PromptText => hasPlayedOnce ? replayPrompt : playPrompt;

    private void Reset()
    {
        AutoAssignReferences();
        TryAutoAssignGameplayReferences();
        TryAutoAssignPcViewTarget();
        TryAutoAssignTimerSlider();
        CacheComponents();
        FitInteractionColliderToRenderers();
        ConfigureVideoPlayer();
    }

    private void Awake()
    {
        AutoAssignReferences();
        TryAutoAssignGameplayReferences();
        TryAutoAssignPcViewTarget();
        TryAutoAssignTimerSlider();
        CacheComponents();
        FitInteractionColliderToRenderers();
        EnsureRuntimeScreenMaterial();
        EnsureScreenRenderTexture();
        SetScreenBlack();
        RefreshTimerUiForIdle();
        RefreshAvailabilityState();
    }

    private void OnEnable()
    {
        DialogueSaveService.CurrentDayChanged += HandleCurrentDayChanged;
        TryAutoAssignGameplayReferences();
        TryAutoAssignPcViewTarget();
        TryAutoAssignTimerSlider();
        RefreshTimerUiForIdle();
        RefreshAvailabilityState();
    }

    private void OnDisable()
    {
        DialogueSaveService.CurrentDayChanged -= HandleCurrentDayChanged;
        StopPlaybackSequenceRoutine();
        StopRestoreViewRoutine();
        StopTimerRoutine(resetUi: true);
        UnsubscribeVideoEvents();
        StopPlayback(resetScreen: true);
        ExitPcViewMode(immediate: true);
    }

    private void OnDestroy()
    {
        StopTimerRoutine(resetUi: true);
        ExitPcViewMode(immediate: true);
        ReleaseScreenRenderTexture();
        ReleaseRuntimeScreenMaterial();
        ReleaseVideoPlayerHost();
    }

    private void OnValidate()
    {
        if ((int)availableFromDay > (int)availableToDay)
        {
            availableToDay = availableFromDay;
        }

        renderTextureWidth = Mathf.Max(256, renderTextureWidth);
        renderTextureHeight = Mathf.Max(256, renderTextureHeight);
        startTimeoutSeconds = Mathf.Max(0.5f, startTimeoutSeconds);
        timerDurationSeconds = Mathf.Max(0.1f, timerDurationSeconds);

        AutoAssignReferences();
        TryAutoAssignGameplayReferences();
        TryAutoAssignPcViewTarget();
        TryAutoAssignTimerSlider();
        CacheComponents();
        FitInteractionColliderToRenderers();
        ConfigureVideoPlayer();
    }

    protected override void Interact()
    {
        if (!CanInteract)
        {
            return;
        }

        EnsureRuntimeScreenMaterial();
        EnsureScreenRenderTexture();
        RecreateVideoPlayerHost();
        ConfigureVideoPlayer();
        SubscribeVideoEvents();

        if (videoPlayer == null || screenRenderTexture == null)
        {
            Debug.LogWarning($"PCVideoInteractable on '{name}' is missing screen video setup.", this);
            SetScreenBlack();
            return;
        }

        if (!TryAssignVideoSource())
        {
            Debug.LogWarning($"PCVideoInteractable on '{name}' has no playable video source.", this);
            SetScreenBlack();
            return;
        }

        StopRestoreViewRoutine();
        StopPlaybackSequenceRoutine();
        isStartingPlayback = true;
        isPlaying = false;
        playbackSequenceRoutine = StartCoroutine(BeginPlaybackSequence());
    }

    protected override void PresentInteraction(PlayerUI playerUI)
    {
        playerUI?.HideInteractionContent();
    }

    private void HandleStarted(VideoPlayer source)
    {
        if (source != videoPlayer)
        {
            return;
        }

        CancelWaitForPlaybackStart();
        isStartingPlayback = false;
        isPlaying = true;
        hasPlayedOnce = true;
        StartPlaybackTimer();
    }

    private void HandleLoopPointReached(VideoPlayer source)
    {
        if (source != videoPlayer)
        {
            return;
        }

        // Video co the ket thuc som hon Timer.
        // Luc nay giu nguyen man hinh/camera va cho Timer quyet dinh khi nao thoat.
        CancelWaitForPlaybackStart();
        isStartingPlayback = false;
        isPlaying = false;
        hasPlayedOnce = true;
    }

    private void HandleErrorReceived(VideoPlayer source, string errorMessage)
    {
        if (source != videoPlayer)
        {
            return;
        }

        isStartingPlayback = false;
        Debug.LogWarning($"PC video on '{name}' failed to play: {errorMessage}", this);
        StopTimerRoutine(resetUi: true);
        StopPlayback(resetScreen: true);
        BeginExitPcViewMode();
    }

    private void HandleCurrentDayChanged(DialogueDay day)
    {
        RefreshAvailabilityState();
    }

    private void RefreshSiblingBaseInteractableState(DialogueDay currentDay)
    {
        Interactable[] interactables = GetComponents<Interactable>();
        bool shouldEnableBaseInteractable = currentDay < DialogueDay.Day6;

        for (int index = 0; index < interactables.Length; index++)
        {
            Interactable candidate = interactables[index];
            if (candidate == null ||
                candidate == this ||
                candidate.GetType() != typeof(Interactable))
            {
                continue;
            }

            candidate.enabled = shouldEnableBaseInteractable;
        }
    }

    private void CacheComponents()
    {
        if (interactionCollider == null)
        {
            interactionCollider = GetComponent<BoxCollider>();
        }
    }

    private void ConfigureVideoPlayer()
    {
        if (videoPlayer == null)
        {
            return;
        }

        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.skipOnDrop = true;
        videoPlayer.waitForFirstFrame = true;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = screenRenderTexture;
    }

    private void SubscribeVideoEvents()
    {
        if (videoPlayer == null)
        {
            return;
        }

        videoPlayer.started -= HandleStarted;
        videoPlayer.loopPointReached -= HandleLoopPointReached;
        videoPlayer.errorReceived -= HandleErrorReceived;

        videoPlayer.started += HandleStarted;
        videoPlayer.loopPointReached += HandleLoopPointReached;
        videoPlayer.errorReceived += HandleErrorReceived;
    }

    private void UnsubscribeVideoEvents()
    {
        if (videoPlayer == null)
        {
            return;
        }

        videoPlayer.started -= HandleStarted;
        videoPlayer.loopPointReached -= HandleLoopPointReached;
        videoPlayer.errorReceived -= HandleErrorReceived;
    }

    private bool TryAssignVideoSource()
    {
        if (videoPlayer == null)
        {
            return false;
        }

        if (videoSourcePriority == VideoSourcePriority.VideoClipFirst)
        {
            if (TryAssignVideoClipSource())
            {
                return true;
            }

            return TryAssignStreamingSource();
        }

        if (TryAssignStreamingSource())
        {
            return true;
        }

        return TryAssignVideoClipSource();
    }

    private void AutoAssignReferences()
    {
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>(true);
        if (renderers.Length == 0)
        {
            return;
        }

        for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
        {
            MeshRenderer candidateRenderer = renderers[rendererIndex];
            Material[] candidateMaterials = candidateRenderer.sharedMaterials;
            for (int materialIndex = 0; materialIndex < candidateMaterials.Length; materialIndex++)
            {
                Material candidateMaterial = candidateMaterials[materialIndex];
                if (candidateMaterial == null || !candidateMaterial.name.Contains("ScreenMat"))
                {
                    continue;
                }

                screenRenderer = candidateRenderer;
                screenMaterialIndex = materialIndex;
                screenMaterialTemplate = candidateMaterial;
                return;
            }
        }

        if (screenRenderer == null)
        {
            screenRenderer = renderers[0];
        }

        Material[] sharedMaterials = screenRenderer.sharedMaterials;
        if (sharedMaterials.Length == 0)
        {
            screenMaterialTemplate = null;
            screenMaterialIndex = 0;
            return;
        }

        screenMaterialIndex = Mathf.Clamp(screenMaterialIndex, 0, sharedMaterials.Length - 1);
        screenMaterialTemplate = sharedMaterials[screenMaterialIndex];
    }

    private void TryAutoAssignGameplayReferences()
    {
        mainCamera ??= Camera.main;

        if (mainCamera == null)
        {
            PlayerLook existingPlayerLook = FindFirstObjectByType<PlayerLook>();
            if (existingPlayerLook != null)
            {
                mainCamera = existingPlayerLook.GetComponent<Camera>();
            }
        }

        playerUI ??= FindFirstObjectByType<PlayerUI>();
        inventoryUIController ??= FindFirstObjectByType<InventoryUIController>();
        playerMovement ??= FindFirstObjectByType<PlayerMovement>();
        jump ??= FindFirstObjectByType<Jump>();
        crouch ??= FindFirstObjectByType<Crouch>();
        actionScript ??= FindFirstObjectByType<ActionScript>();
        playerRigidbody ??= playerMovement != null
            ? playerMovement.GetComponent<Rigidbody>()
            : FindFirstObjectByType<Rigidbody>();

        if (mainCamera != null)
        {
            playerLook ??= mainCamera.GetComponent<PlayerLook>();
            zoom ??= mainCamera.GetComponent<Zoom>();
            pickUpScript ??= mainCamera.GetComponent<PickUpScript>();
            cuttingTreeSystem ??= mainCamera.GetComponent<CuttingTreeSystem>();
        }
    }

    private void TryAutoAssignPcViewTarget()
    {
        if (pcViewTarget != null || string.IsNullOrWhiteSpace(pcViewObjectName))
        {
            return;
        }

        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int index = 0; index < transforms.Length; index++)
        {
            Transform candidate = transforms[index];
            if (candidate != null &&
                string.Equals(candidate.name, pcViewObjectName, System.StringComparison.OrdinalIgnoreCase))
            {
                pcViewTarget = candidate;
                return;
            }
        }
    }

    private void TryAutoAssignTimerSlider()
    {
        if (timerSlider != null || string.IsNullOrWhiteSpace(timerObjectName))
        {
            return;
        }

        Slider[] sliders = FindObjectsByType<Slider>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int index = 0; index < sliders.Length; index++)
        {
            Slider candidate = sliders[index];
            if (candidate != null &&
                string.Equals(candidate.name, timerObjectName, System.StringComparison.OrdinalIgnoreCase))
            {
                timerSlider = candidate;
                return;
            }
        }

        for (int index = 0; index < sliders.Length; index++)
        {
            Slider candidate = sliders[index];
            if (candidate != null &&
                candidate.transform.parent != null &&
                string.Equals(candidate.transform.parent.name, timerObjectName, System.StringComparison.OrdinalIgnoreCase))
            {
                timerSlider = candidate;
                return;
            }
        }
    }

    private void FitInteractionColliderToRenderers()
    {
        if (interactionCollider == null)
        {
            return;
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        Bounds localBounds = default;

        for (int index = 0; index < renderers.Length; index++)
        {
            Renderer rendererComponent = renderers[index];
            Bounds worldBounds = rendererComponent.bounds;
            Vector3 extents = worldBounds.extents;

            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 worldPoint = worldBounds.center + Vector3.Scale(extents, new Vector3(x, y, z));
                        Vector3 localPoint = transform.InverseTransformPoint(worldPoint);

                        if (!hasBounds)
                        {
                            localBounds = new Bounds(localPoint, Vector3.zero);
                            hasBounds = true;
                            continue;
                        }

                        localBounds.Encapsulate(localPoint);
                    }
                }
            }
        }

        if (!hasBounds)
        {
            return;
        }

        interactionCollider.isTrigger = true;
        interactionCollider.center = localBounds.center;
        interactionCollider.size = localBounds.size;
    }

    private void EnsureRuntimeScreenMaterial()
    {
        if (!Application.isPlaying || screenRenderer == null || screenMaterialTemplate == null)
        {
            return;
        }

        if (runtimeScreenMaterial == null)
        {
            runtimeScreenMaterial = new Material(screenMaterialTemplate)
            {
                name = $"{screenMaterialTemplate.name}_Runtime"
            };

            Material[] materials = screenRenderer.materials;
            if (screenMaterialIndex >= materials.Length)
            {
                return;
            }

            materials[screenMaterialIndex] = runtimeScreenMaterial;
            screenRenderer.materials = materials;
        }

        if (runtimeScreenMaterial.HasProperty(BaseColorId))
        {
            runtimeScreenMaterial.SetColor(BaseColorId, Color.white);
        }

        if (runtimeScreenMaterial.HasProperty(ColorId))
        {
            runtimeScreenMaterial.SetColor(ColorId, Color.white);
        }
    }

    private void EnsureScreenRenderTexture()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        bool needsNewTexture =
            screenRenderTexture == null ||
            screenRenderTexture.width != renderTextureWidth ||
            screenRenderTexture.height != renderTextureHeight;

        if (needsNewTexture)
        {
            ReleaseScreenRenderTexture();
            screenRenderTexture = new RenderTexture(renderTextureWidth, renderTextureHeight, 0, RenderTextureFormat.ARGB32)
            {
                name = $"{name}_PCScreenVideo"
            };
            screenRenderTexture.Create();
        }

        ApplyScreenTexture(screenRenderTexture);

        if (videoPlayer != null)
        {
            videoPlayer.targetTexture = screenRenderTexture;
        }
    }

    private void ApplyScreenTexture(Texture texture)
    {
        if (runtimeScreenMaterial == null || texture == null)
        {
            return;
        }

        if (runtimeScreenMaterial.HasProperty(BaseMapId))
        {
            runtimeScreenMaterial.SetTexture(BaseMapId, texture);
        }

        if (runtimeScreenMaterial.HasProperty(MainTexId))
        {
            runtimeScreenMaterial.SetTexture(MainTexId, texture);
        }
    }

    private void SetScreenBlack()
    {
        if (screenRenderTexture == null)
        {
            return;
        }

        ClearRenderTexture(screenRenderTexture, idleScreenColor);
    }

    private void StopPlayback(bool resetScreen)
    {
        CancelWaitForPlaybackStart();
        isStartingPlayback = false;
        isPlaying = false;

        if (videoPlayer != null)
        {
            videoPlayer.Stop();
        }

        if (resetScreen)
        {
            SetScreenBlack();
        }
    }

    private IEnumerator BeginPlaybackSequence()
    {
        TryAutoAssignGameplayReferences();
        TryAutoAssignPcViewTarget();

        if (!EnterPcViewMode())
        {
            isStartingPlayback = false;
            playbackSequenceRoutine = null;
            yield break;
        }

        Transform target = ResolvePcViewTarget();
        if (target != null)
        {
            yield return MoveCameraToWorldPose(target.position, target.rotation, moveToPcViewDuration);
        }

        if (videoPlayer == null)
        {
            isStartingPlayback = false;
            BeginExitPcViewMode();
            playbackSequenceRoutine = null;
            yield break;
        }

        CancelWaitForPlaybackStart();
        videoPlayer.Play();
        waitForPlaybackStartRoutine = StartCoroutine(WaitForPlaybackStartTimeout());
        playbackSequenceRoutine = null;
    }

    private bool EnterPcViewMode()
    {
        if (isViewingPc)
        {
            return true;
        }

        if (mainCamera == null)
        {
            return false;
        }

        if (closeInventoryBeforePlayback &&
            inventoryUIController != null &&
            inventoryUIController.IsInventoryOpen)
        {
            inventoryUIController.SetInventoryOpen(false);
        }

        CacheCameraPose();
        CacheCursorState();
        CacheAndDisableControls();

        if (playerRigidbody != null)
        {
            cachedRigidbodyConstraints = playerRigidbody.constraints;
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
            playerRigidbody.constraints = RigidbodyConstraints.FreezeAll;
        }

        if (playerUI != null)
        {
            playerUI.UpdatePrompt(string.Empty);
            playerUI.HideInteractionContent();
        }

        SetTimerProgress(0f);
        SetTimerVisible(true);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Input.ResetInputAxes();
        isViewingPc = true;
        return true;
    }

    private void BeginExitPcViewMode()
    {
        if (!isViewingPc)
        {
            return;
        }

        StopTimerRoutine(resetUi: true);
        StopRestoreViewRoutine();
        if (!isActiveAndEnabled)
        {
            ExitPcViewMode(immediate: true);
            return;
        }

        restoreViewRoutine = StartCoroutine(RestoreFromPcViewRoutine());
    }

    private IEnumerator RestoreFromPcViewRoutine()
    {
        yield return RestoreCameraPose(returnFromPcViewDuration);
        ExitPcViewMode(immediate: true);
        restoreViewRoutine = null;
    }

    private void ExitPcViewMode(bool immediate)
    {
        if (!isViewingPc && !hasCachedCameraPose && cachedControlStates.Count == 0)
        {
            return;
        }

        if (immediate)
        {
            RestoreCameraPoseImmediate();
        }

        if (playerRigidbody != null)
        {
            playerRigidbody.constraints = cachedRigidbodyConstraints;
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
        }

        RestoreControls();
        RestoreCursorState();
        RefreshTimerUiForIdle();
        Input.ResetInputAxes();
        isViewingPc = false;
    }

    private bool IsAvailableForCurrentDay()
    {
        DialogueDay currentDay = DialogueController.GetCurrentDay();
        return currentDay >= availableFromDay && currentDay <= availableToDay;
    }

    private void RefreshAvailabilityState()
    {
        if (interactionCollider == null)
        {
            CacheComponents();
        }

        DialogueDay currentDay = DialogueController.GetCurrentDay();
        RefreshSiblingBaseInteractableState(currentDay);

        bool isAvailableToday = currentDay >= availableFromDay && currentDay <= availableToDay;
        if (!isAvailableToday)
        {
            StopTimerRoutine(resetUi: true);
            StopPlayback(resetScreen: true);
            ExitPcViewMode(immediate: true);
        }

        if (interactionCollider != null && disableColliderWhenUnavailable)
        {
            interactionCollider.enabled = isAvailableToday;
        }
    }

    private bool HasAnyVideoSource()
    {
        if (videoClip != null)
        {
            return true;
        }

        string streamingPath = ResolveStreamingVideoPath();
        return !string.IsNullOrWhiteSpace(streamingPath) && File.Exists(streamingPath);
    }

    private bool TryAssignVideoClipSource()
    {
        if (videoPlayer == null || videoClip == null)
        {
            return false;
        }

        videoPlayer.source = VideoSource.VideoClip;
        videoPlayer.clip = videoClip;
        videoPlayer.url = string.Empty;
        return true;
    }

    private bool TryAssignStreamingSource()
    {
        if (videoPlayer == null)
        {
            return false;
        }

        string streamingPath = ResolveStreamingVideoPath();
        if (string.IsNullOrWhiteSpace(streamingPath) || !File.Exists(streamingPath))
        {
            return false;
        }

        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = streamingPath;
        videoPlayer.clip = null;
        return true;
    }

    private string ResolveStreamingVideoPath()
    {
        if (string.IsNullOrWhiteSpace(streamingVideoRelativePath))
        {
            return string.Empty;
        }

        if (Path.IsPathRooted(streamingVideoRelativePath))
        {
            return streamingVideoRelativePath;
        }

        string relativePath = streamingVideoRelativePath
            .Replace('\\', Path.DirectorySeparatorChar)
            .Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(Application.streamingAssetsPath, relativePath);
    }

    private IEnumerator WaitForPlaybackStartTimeout()
    {
        float timeout = Mathf.Max(0.5f, startTimeoutSeconds);
        float elapsed = 0f;

        while (elapsed < timeout)
        {
            if (!isStartingPlayback || isPlaying)
            {
                waitForPlaybackStartRoutine = null;
                yield break;
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        waitForPlaybackStartRoutine = null;
        if (!isPlaying && isStartingPlayback)
        {
            Debug.LogWarning($"PC video on '{name}' did not start within {timeout:0.0}s.", this);
            StopTimerRoutine(resetUi: true);
            StopPlayback(resetScreen: true);
            BeginExitPcViewMode();
        }
    }

    private void CancelWaitForPlaybackStart()
    {
        if (waitForPlaybackStartRoutine == null)
        {
            return;
        }

        StopCoroutine(waitForPlaybackStartRoutine);
        waitForPlaybackStartRoutine = null;
    }

    private void StopPlaybackSequenceRoutine()
    {
        if (playbackSequenceRoutine == null)
        {
            return;
        }

        StopCoroutine(playbackSequenceRoutine);
        playbackSequenceRoutine = null;
    }

    private void StopRestoreViewRoutine()
    {
        if (restoreViewRoutine == null)
        {
            return;
        }

        StopCoroutine(restoreViewRoutine);
        restoreViewRoutine = null;
    }

    private void StartPlaybackTimer()
    {
        StopTimerRoutine(resetUi: false);
        SetTimerProgress(0f);
        SetTimerVisible(true);
        timerRoutine = StartCoroutine(RunPlaybackTimer());
    }

    private IEnumerator RunPlaybackTimer()
    {
        float duration = Mathf.Max(0.1f, timerDurationSeconds);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (!isViewingPc)
            {
                timerRoutine = null;
                yield break;
            }

            elapsed += Time.unscaledDeltaTime;
            SetTimerProgress(elapsed / duration);
            yield return null;
        }

        SetTimerProgress(1f);
        timerRoutine = null;
        HandlePlaybackTimerCompleted();
    }

    private void HandlePlaybackTimerCompleted()
    {
        if (!isViewingPc)
        {
            return;
        }

        StopPlayback(resetScreen: true);
        BeginExitPcViewMode();
    }

    private void StopTimerRoutine(bool resetUi)
    {
        if (timerRoutine != null)
        {
            StopCoroutine(timerRoutine);
            timerRoutine = null;
        }

        if (resetUi)
        {
            RefreshTimerUiForIdle();
        }
    }

    private void RefreshTimerUiForIdle()
    {
        SetTimerProgress(0f);
        SetTimerVisible(!hideTimerWhenInactive);
    }

    private void SetTimerProgress(float normalizedValue)
    {
        TryAutoAssignTimerSlider();
        if (timerSlider == null)
        {
            return;
        }

        timerSlider.minValue = 0f;
        timerSlider.maxValue = 1f;
        timerSlider.wholeNumbers = false;
        timerSlider.normalizedValue = Mathf.Clamp01(normalizedValue);
    }

    private void SetTimerVisible(bool isVisible)
    {
        TryAutoAssignTimerSlider();
        if (timerSlider == null)
        {
            return;
        }

        timerSlider.gameObject.SetActive(isVisible);
    }

    private void ReleaseScreenRenderTexture()
    {
        if (screenRenderTexture == null)
        {
            return;
        }

        screenRenderTexture.Release();

        if (Application.isPlaying)
        {
            Destroy(screenRenderTexture);
        }
        else
        {
            DestroyImmediate(screenRenderTexture);
        }

        screenRenderTexture = null;
    }

    private void ReleaseRuntimeScreenMaterial()
    {
        if (runtimeScreenMaterial == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(runtimeScreenMaterial);
        }
        else
        {
            DestroyImmediate(runtimeScreenMaterial);
        }

        runtimeScreenMaterial = null;
    }

    private static void ClearRenderTexture(RenderTexture target, Color clearColor)
    {
        if (target == null)
        {
            return;
        }

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = target;
        GL.Clear(true, true, clearColor);
        RenderTexture.active = previous;
    }

    private void RecreateVideoPlayerHost()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        ReleaseVideoPlayerHost();
        videoPlayerHost = new GameObject($"{name}_VideoPlayerHost");
        videoPlayer = videoPlayerHost.AddComponent<VideoPlayer>();
    }

    private void ReleaseVideoPlayerHost()
    {
        if (videoPlayerHost == null)
        {
            videoPlayer = null;
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(videoPlayerHost);
        }
        else
        {
            DestroyImmediate(videoPlayerHost);
        }

        videoPlayerHost = null;
        videoPlayer = null;
    }

    private Transform ResolvePcViewTarget()
    {
        if (pcViewTarget != null)
        {
            return pcViewTarget;
        }

        TryAutoAssignPcViewTarget();
        if (pcViewTarget != null)
        {
            return pcViewTarget;
        }

        if (screenRenderer != null)
        {
            return screenRenderer.transform;
        }

        return transform;
    }

    private void CacheAndDisableControls()
    {
        cachedControlStates.Clear();
        CacheBehaviour(playerMovement);
        CacheBehaviour(jump);
        CacheBehaviour(crouch);
        CacheBehaviour(playerLook);
        CacheBehaviour(zoom);
        CacheBehaviour(pickUpScript);
        CacheBehaviour(actionScript);
        CacheBehaviour(cuttingTreeSystem);
        CacheBehaviour(inventoryUIController);
    }

    private void RestoreControls()
    {
        foreach (KeyValuePair<Behaviour, bool> state in cachedControlStates)
        {
            if (state.Key != null)
            {
                state.Key.enabled = state.Value;
            }
        }

        cachedControlStates.Clear();
    }

    private void CacheBehaviour(Behaviour behaviour)
    {
        if (behaviour == null || behaviour == this || cachedControlStates.ContainsKey(behaviour))
        {
            return;
        }

        cachedControlStates.Add(behaviour, behaviour.enabled);
        behaviour.enabled = false;
    }

    private void CacheCameraPose()
    {
        if (hasCachedCameraPose || mainCamera == null)
        {
            return;
        }

        cachedCameraLocalPosition = mainCamera.transform.localPosition;
        cachedCameraLocalRotation = mainCamera.transform.localRotation;
        hasCachedCameraPose = true;
    }

    private IEnumerator RestoreCameraPose(float duration)
    {
        if (!hasCachedCameraPose || mainCamera == null)
        {
            yield break;
        }

        Transform parent = mainCamera.transform.parent;
        Vector3 targetPosition = parent != null
            ? parent.TransformPoint(cachedCameraLocalPosition)
            : cachedCameraLocalPosition;
        Quaternion targetRotation = parent != null
            ? parent.rotation * cachedCameraLocalRotation
            : cachedCameraLocalRotation;

        yield return MoveCameraToWorldPose(targetPosition, targetRotation, duration);
        RestoreCameraPoseImmediate();
    }

    private void RestoreCameraPoseImmediate()
    {
        if (!hasCachedCameraPose || mainCamera == null)
        {
            return;
        }

        mainCamera.transform.localPosition = cachedCameraLocalPosition;
        mainCamera.transform.localRotation = cachedCameraLocalRotation;
        hasCachedCameraPose = false;
    }

    private IEnumerator MoveCameraToWorldPose(Vector3 targetPosition, Quaternion targetRotation, float duration)
    {
        if (mainCamera == null)
        {
            yield break;
        }

        Transform cameraTransform = mainCamera.transform;
        Vector3 startPosition = cameraTransform.position;
        Quaternion startRotation = cameraTransform.rotation;

        if (duration <= 0f)
        {
            cameraTransform.SetPositionAndRotation(targetPosition, targetRotation);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * (3f - 2f * t);
            cameraTransform.SetPositionAndRotation(
                Vector3.Lerp(startPosition, targetPosition, t),
                Quaternion.Slerp(startRotation, targetRotation, t));
            yield return null;
        }

        cameraTransform.SetPositionAndRotation(targetPosition, targetRotation);
    }

    private void CacheCursorState()
    {
        if (hasCachedCursorState)
        {
            return;
        }

        cachedCursorLockState = Cursor.lockState;
        cachedCursorVisible = Cursor.visible;
        hasCachedCursorState = true;
    }

    private void RestoreCursorState()
    {
        if (!hasCachedCursorState)
        {
            return;
        }

        Cursor.lockState = cachedCursorLockState;
        Cursor.visible = cachedCursorVisible;
        hasCachedCursorState = false;
    }
}
