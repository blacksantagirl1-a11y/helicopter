using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Video;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public sealed class PCVideoInteractable : Interactable
{
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

    [Header("Screen")]
    [SerializeField] private MeshRenderer screenRenderer;
    [SerializeField] [Min(0)] private int screenMaterialIndex = 1;
    [SerializeField] private Material screenMaterialTemplate;
    [SerializeField] [Min(256)] private int renderTextureWidth = 1024;
    [SerializeField] [Min(256)] private int renderTextureHeight = 1024;
    [SerializeField] private Color idleScreenColor = Color.black;

    [Header("Video")]
    [SerializeField] private string streamingVideoRelativePath = "Videos/PCScreen.mp4";
    [SerializeField] private VideoClip videoClip;
    [SerializeField] [Min(0.5f)] private float startTimeoutSeconds = 10f;

    private GameObject videoPlayerHost;
    private VideoPlayer videoPlayer;
    private BoxCollider interactionCollider;
    private RenderTexture screenRenderTexture;
    private Material runtimeScreenMaterial;
    private Coroutine waitForPlaybackStartRoutine;
    private bool isStartingPlayback;
    private bool isPlaying;
    private bool hasPlayedOnce;

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
        CacheComponents();
        FitInteractionColliderToRenderers();
        ConfigureVideoPlayer();
    }

    private void Awake()
    {
        AutoAssignReferences();
        CacheComponents();
        FitInteractionColliderToRenderers();
        EnsureRuntimeScreenMaterial();
        EnsureScreenRenderTexture();
        SetScreenBlack();
        RefreshAvailabilityState();
    }

    private void OnEnable()
    {
        DialogueSaveService.CurrentDayChanged += HandleCurrentDayChanged;
        RefreshAvailabilityState();
    }

    private void OnDisable()
    {
        DialogueSaveService.CurrentDayChanged -= HandleCurrentDayChanged;
        UnsubscribeVideoEvents();
        StopPlayback(resetScreen: true);
    }

    private void OnDestroy()
    {
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

        AutoAssignReferences();
        CacheComponents();
        FitInteractionColliderToRenderers();
        ConfigureVideoPlayer();
    }

    protected override void Interact()
    {
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

        isStartingPlayback = true;
        isPlaying = false;
        CancelWaitForPlaybackStart();
        videoPlayer.Play();
        waitForPlaybackStartRoutine = StartCoroutine(WaitForPlaybackStartTimeout());
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
    }

    private void HandleLoopPointReached(VideoPlayer source)
    {
        if (source != videoPlayer)
        {
            return;
        }

        StopPlayback(resetScreen: true);
    }

    private void HandleErrorReceived(VideoPlayer source, string errorMessage)
    {
        if (source != videoPlayer)
        {
            return;
        }

        isStartingPlayback = false;
        Debug.LogWarning($"PC video on '{name}' failed to play: {errorMessage}", this);
        StopPlayback(resetScreen: true);
    }

    private void HandleCurrentDayChanged(DialogueDay day)
    {
        RefreshAvailabilityState();
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

        string streamingPath = ResolveStreamingVideoPath();
        if (!string.IsNullOrWhiteSpace(streamingPath) && File.Exists(streamingPath))
        {
            videoPlayer.source = VideoSource.Url;
            videoPlayer.url = streamingPath;
            videoPlayer.clip = null;
            return true;
        }

        if (videoClip == null)
        {
            return false;
        }

        videoPlayer.source = VideoSource.VideoClip;
        videoPlayer.clip = videoClip;
        videoPlayer.url = string.Empty;
        return true;
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

        bool isAvailableToday = IsAvailableForCurrentDay();
        if (!isAvailableToday)
        {
            StopPlayback(resetScreen: true);
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
            StopPlayback(resetScreen: true);
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
}
