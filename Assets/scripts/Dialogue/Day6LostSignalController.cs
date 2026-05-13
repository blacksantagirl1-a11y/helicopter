using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class Day6LostSignalController : MonoBehaviour
{
    private const string ControllerObjectName = "Day6LostSignalController";
    private const string OverlayObjectName = "Day6LostSignalOverlay";
    private const string OverlayTextObjectName = "Day6LostSignalText";

    private static bool installerRegistered;
    private static Day6LostSignalController instance;

    [Header("Glitch")]
    [SerializeField] [Min(0.1f)] private float glitchRampDurationSeconds = 100f;
    [SerializeField] [Range(0f, 1f)] private float maxGlitchAmount = 0.4f;

    [Header("Lost Signal")]
    [SerializeField] private string lostSignalMessage = "Lost Signal";
    [SerializeField] [Min(0f)] private float lostSignalHoldSeconds = 1.5f;
    [SerializeField] private Color lostSignalBackgroundColor = new Color(0f, 0f, 0f, 0.95f);
    [SerializeField] private Color lostSignalTextColor = new Color(0.95f, 0.98f, 1f, 1f);
    [SerializeField] [Min(1f)] private float lostSignalFontSize = 72f;

    private Coroutine glitchRoutine;
    private Coroutine reloadRoutine;
    private bool hasTriggeredThisScene;
    private GameObject overlayRoot;
    private CanvasGroup overlayCanvasGroup;
    private TextMeshProUGUI overlayText;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetInstallerState()
    {
        installerRegistered = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InstallAfterSceneLoad()
    {
        RegisterSceneCallbacks();
        TryInstallControllerIfNeeded();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        glitchRampDurationSeconds = Mathf.Max(0.1f, glitchRampDurationSeconds);
        maxGlitchAmount = Mathf.Clamp01(maxGlitchAmount);
        lostSignalHoldSeconds = Mathf.Max(0f, lostSignalHoldSeconds);
        lostSignalFontSize = Mathf.Max(1f, lostSignalFontSize);
        HideOverlayImmediate();
        HintDay3KinoGlitchState.Clear();
    }

    private void OnEnable()
    {
        DialogueSaveService.CurrentDayChanged += HandleCurrentDayChanged;
        TryStartDay6Sequence();
    }

    private void OnDisable()
    {
        DialogueSaveService.CurrentDayChanged -= HandleCurrentDayChanged;
        CancelAllRoutines();
        hasTriggeredThisScene = false;
        ResetVisualState();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }

        CancelAllRoutines();
        hasTriggeredThisScene = false;
        ResetVisualState();
    }

    private void OnValidate()
    {
        glitchRampDurationSeconds = Mathf.Max(0.1f, glitchRampDurationSeconds);
        maxGlitchAmount = Mathf.Clamp01(maxGlitchAmount);
        lostSignalHoldSeconds = Mathf.Max(0f, lostSignalHoldSeconds);
        lostSignalFontSize = Mathf.Max(1f, lostSignalFontSize);
    }

    private static void RegisterSceneCallbacks()
    {
        if (installerRegistered)
        {
            return;
        }

        SceneManager.sceneLoaded += HandleSceneLoaded;
        installerRegistered = true;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryInstallControllerIfNeeded();
    }

    private static void TryInstallControllerIfNeeded()
    {
        if (FindFirstObjectByType<Day6LostSignalController>(FindObjectsInactive.Include) != null)
        {
            return;
        }

        if (!HasGameplayMarkers())
        {
            return;
        }

        GameObject controllerObject = new GameObject(ControllerObjectName);
        controllerObject.AddComponent<Day6LostSignalController>();
    }

    private static bool HasGameplayMarkers()
    {
        return FindFirstObjectByType<PlayerMovement>(FindObjectsInactive.Include) != null;
    }

    private void HandleCurrentDayChanged(DialogueDay day)
    {
        if (day == DialogueDay.Day6)
        {
            TryStartDay6Sequence();
            return;
        }

        CancelAllRoutines();
        hasTriggeredThisScene = false;
        ResetVisualState();
    }

    private void TryStartDay6Sequence()
    {
        if (!isActiveAndEnabled ||
            hasTriggeredThisScene ||
            glitchRoutine != null ||
            reloadRoutine != null)
        {
            return;
        }

        if (DialogueSaveService.GetCurrentDay() != DialogueDay.Day6 || !HasGameplayMarkers())
        {
            return;
        }

        hasTriggeredThisScene = true;
        glitchRoutine = StartCoroutine(RunDay6GlitchSequence());
    }

    private IEnumerator RunDay6GlitchSequence()
    {
        HideOverlayImmediate();
        SetGlitchAmount(0f);

        float duration = Mathf.Max(0.1f, glitchRampDurationSeconds);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (DialogueSaveService.GetCurrentDay() != DialogueDay.Day6)
            {
                glitchRoutine = null;
                hasTriggeredThisScene = false;
                ResetVisualState();
                yield break;
            }

            elapsed += Time.unscaledDeltaTime;
            SetGlitchAmount(elapsed / duration);
            yield return null;
        }

        SetGlitchAmount(1f);
        glitchRoutine = null;
        reloadRoutine = StartCoroutine(ShowLostSignalAndReplayDay6());
    }

    private IEnumerator ShowLostSignalAndReplayDay6()
    {
        ShowOverlay();

        if (lostSignalHoldSeconds > 0f)
        {
            yield return new WaitForSecondsRealtime(lostSignalHoldSeconds);
        }

        string sceneName = SceneManager.GetActiveScene().name;
        if (!LoadingManager.LoadScene(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }

        reloadRoutine = null;
    }

    private void CancelAllRoutines()
    {
        if (glitchRoutine != null)
        {
            StopCoroutine(glitchRoutine);
            glitchRoutine = null;
        }

        if (reloadRoutine != null)
        {
            StopCoroutine(reloadRoutine);
            reloadRoutine = null;
        }
    }

    private void ResetVisualState()
    {
        HintDay3KinoGlitchState.Clear();
        HideOverlayImmediate();
    }

    private void SetGlitchAmount(float normalizedValue)
    {
        HintDay3KinoGlitchState.SetAmount(Mathf.Clamp01(normalizedValue) * maxGlitchAmount);
    }

    private void EnsureOverlay()
    {
        if (overlayRoot != null && overlayCanvasGroup != null && overlayText != null)
        {
            ConfigureOverlay();
            return;
        }

        TMP_FontAsset font = TMP_Settings.defaultFontAsset ?? Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        if (overlayRoot == null)
        {
            overlayRoot = new GameObject(
                OverlayObjectName,
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(CanvasGroup));
            overlayRoot.transform.SetParent(transform, false);

            Canvas canvas = overlayRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 6000;

            CanvasScaler scaler = overlayRoot.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject backdropObject = new GameObject("Backdrop", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            backdropObject.transform.SetParent(overlayRoot.transform, false);

            GameObject textObject = new GameObject(
                OverlayTextObjectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            textObject.transform.SetParent(overlayRoot.transform, false);
            overlayText = textObject.GetComponent<TextMeshProUGUI>();

            if (font != null)
            {
                overlayText.font = font;
            }
        }

        overlayCanvasGroup ??= overlayRoot.GetComponent<CanvasGroup>();
        overlayText ??= overlayRoot.GetComponentInChildren<TextMeshProUGUI>(true);
        ConfigureOverlay();
    }

    private void ConfigureOverlay()
    {
        if (overlayRoot == null)
        {
            return;
        }

        RectTransform overlayRect = overlayRoot.GetComponent<RectTransform>();
        Stretch(overlayRect);

        Image[] images = overlayRoot.GetComponentsInChildren<Image>(true);
        for (int index = 0; index < images.Length; index++)
        {
            Image image = images[index];
            if (image == null)
            {
                continue;
            }

            Stretch(image.rectTransform);
            image.color = lostSignalBackgroundColor;
            image.raycastTarget = false;
        }

        if (overlayCanvasGroup != null)
        {
            overlayCanvasGroup.alpha = 0f;
            overlayCanvasGroup.interactable = false;
            overlayCanvasGroup.blocksRaycasts = false;
        }

        if (overlayText == null)
        {
            return;
        }

        RectTransform textRect = overlayText.rectTransform;
        Stretch(textRect);
        textRect.offsetMin = new Vector2(48f, 48f);
        textRect.offsetMax = new Vector2(-48f, -48f);

        overlayText.text = string.IsNullOrWhiteSpace(lostSignalMessage) ? "Lost Signal" : lostSignalMessage;
        overlayText.fontSize = lostSignalFontSize;
        overlayText.fontStyle = FontStyles.Bold;
        overlayText.alignment = TextAlignmentOptions.Center;
        overlayText.color = lostSignalTextColor;
        overlayText.raycastTarget = false;
    }

    private void ShowOverlay()
    {
        EnsureOverlay();
        if (overlayRoot == null)
        {
            return;
        }

        overlayRoot.SetActive(true);
        overlayRoot.transform.SetAsLastSibling();

        if (overlayCanvasGroup != null)
        {
            overlayCanvasGroup.alpha = 1f;
        }
    }

    private void HideOverlayImmediate()
    {
        if (overlayCanvasGroup != null)
        {
            overlayCanvasGroup.alpha = 0f;
        }

        if (overlayRoot != null)
        {
            overlayRoot.SetActive(false);
        }
    }

    private static void Stretch(RectTransform rectTransform)
    {
        if (rectTransform == null)
        {
            return;
        }

        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.localScale = Vector3.one;
    }
}
