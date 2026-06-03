using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingManager : MonoBehaviour
{
    const string LoadingText = "Loading...";

    static LoadingManager instance;

    [Header("Overlay")]
    [Tooltip("Root của overlay loading (canvas/panel chứa nền và chữ loading)")]
    [SerializeField] GameObject overlayRoot;
    [Tooltip("CanvasGroup dùng để fade in/out overlay")]
    [SerializeField] CanvasGroup overlayCanvasGroup;
    [Tooltip("Nhãn chữ hiển thị trạng thái loading")]
    [SerializeField] TMP_Text loadingLabel;

    [Header("Timings")]
    [Tooltip("Thời gian fade vào của overlay (giây)")]
    [SerializeField, Min(0f)] float fadeInDuration = 0.25f;
    [Tooltip("Thời gian fade ra của overlay (giây)")]
    [SerializeField, Min(0f)] float fadeOutDuration = 0.5f;
    [Tooltip("Thời gian tối thiểu overlay cần hiển thị để tránh chớp màn hình (giây)")]
    [SerializeField, Min(0f)] float minimumVisibleDuration = 0.45f;

    Coroutine activeLoadRoutine;
    Tween overlayFadeTween;
    bool isLoading;

    public static bool IsLoading => instance != null && instance.isLoading;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        HideOverlayImmediate();
    }

    void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }

        overlayFadeTween?.Kill();
    }

    public static bool LoadScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("LoadingManager received an empty scene name.");
            return false;
        }

        LoadingManager manager = EnsureInstance();
        return manager != null && manager.TryBeginLoad(sceneName);
    }

    bool TryBeginLoad(string sceneName)
    {
        if (isLoading || activeLoadRoutine != null)
        {
            return false;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"Scene '{sceneName}' is not available in build settings.");
            return false;
        }

        activeLoadRoutine = StartCoroutine(LoadSceneRoutine(sceneName));
        return true;
    }

    IEnumerator LoadSceneRoutine(string sceneName)
    {
        isLoading = true;
        EnsureFallbackUi();

        ShowOverlayForLoad();
        StartLoadingTextAnimation();

        yield return FadeOverlayRoutine(1f, fadeInDuration);

        float visibleStartedAt = Time.unscaledTime;
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);

        if (asyncLoad == null)
        {
            Debug.LogError($"Failed to start async load for scene '{sceneName}'.");
            StopLoadingTextAnimation();
            yield return FadeOverlayRoutine(0f, fadeOutDuration);
            HideOverlayImmediate();
            isLoading = false;
            activeLoadRoutine = null;
            yield break;
        }

        asyncLoad.allowSceneActivation = false;

        while (asyncLoad.progress < 0.9f)
        {
            yield return null;
        }

        float visibleDuration = Time.unscaledTime - visibleStartedAt;
        if (visibleDuration < minimumVisibleDuration)
        {
            yield return new WaitForSecondsRealtime(minimumVisibleDuration - visibleDuration);
        }

        asyncLoad.allowSceneActivation = true;

        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        yield return null;

        StopLoadingTextAnimation();
        yield return FadeOverlayRoutine(0f, fadeOutDuration);

        HideOverlayImmediate();
        isLoading = false;
        activeLoadRoutine = null;
    }

    IEnumerator FadeOverlayRoutine(float targetAlpha, float duration)
    {
        if (overlayCanvasGroup == null)
        {
            yield break;
        }

        overlayFadeTween?.Kill();

        duration = Mathf.Max(0.01f, duration);
        overlayFadeTween = overlayCanvasGroup
            .DOFade(targetAlpha, duration)
            .SetEase(targetAlpha >= overlayCanvasGroup.alpha ? Ease.OutCubic : Ease.InCubic)
            .SetUpdate(true);

        yield return overlayFadeTween.WaitForCompletion();
        overlayFadeTween = null;
    }

    void ShowOverlayForLoad()
    {
        EnsureFallbackUi();
        PrepareOverlayForDisplay();

        if (overlayRoot != null)
        {
            overlayRoot.SetActive(true);
        }

        if (overlayCanvasGroup != null)
        {
            overlayCanvasGroup.alpha = 0f;
            overlayCanvasGroup.interactable = false;
            overlayCanvasGroup.blocksRaycasts = true;
        }
    }

    void HideOverlayImmediate()
    {
        StopLoadingTextAnimation();
        overlayFadeTween?.Kill();
        overlayFadeTween = null;

        EnsureFallbackUi();

        if (overlayCanvasGroup != null)
        {
            overlayCanvasGroup.alpha = 0f;
            overlayCanvasGroup.interactable = false;
            overlayCanvasGroup.blocksRaycasts = false;
        }

        if (overlayRoot != null)
        {
            overlayRoot.SetActive(false);
        }
    }

    void StartLoadingTextAnimation()
    {
        SetLoadingText();
    }

    void StopLoadingTextAnimation()
    {
        // Intentionally empty: loading text is static.
    }

    void SetLoadingText()
    {
        if (loadingLabel == null)
        {
            return;
        }

        loadingLabel.text = LoadingText;
    }

    void PrepareOverlayForDisplay()
    {
        if (overlayRoot == null)
        {
            return;
        }

        RectTransform overlayRect = overlayRoot.GetComponent<RectTransform>();
        if (overlayRect != null)
        {
            Stretch(overlayRect);
        }

        Canvas canvas = overlayRoot.GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5000;
        }

        CanvasScaler scaler = overlayRoot.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        overlayRoot.transform.SetAsLastSibling();
    }

    void EnsureFallbackUi()
    {
        if (overlayRoot != null && overlayCanvasGroup != null && loadingLabel != null)
        {
            return;
        }

        BuildFallbackUi();
    }

    void BuildFallbackUi()
    {
        TMP_FontAsset font = TMP_Settings.defaultFontAsset ?? Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        if (overlayRoot == null || overlayCanvasGroup == null)
        {
            GameObject canvasObject = new("LoadingCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5000;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            overlayRoot = canvasObject;
            overlayCanvasGroup = canvasObject.GetComponent<CanvasGroup>();

            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            Stretch(canvasRect);

            Image backdrop = CreateImage("Backdrop", canvasRect, new Color(0.02f, 0.03f, 0.05f, 0.96f));
            Stretch(backdrop.rectTransform);
        }

        if (loadingLabel == null)
        {
            RectTransform parent = overlayRoot.GetComponent<RectTransform>();
            loadingLabel = CreateText("LoadingLabel", parent, LoadingText, 34f, FontStyles.Bold, TextAlignmentOptions.BottomRight, font);
            loadingLabel.rectTransform.anchorMin = new Vector2(1f, 0f);
            loadingLabel.rectTransform.anchorMax = new Vector2(1f, 0f);
            loadingLabel.rectTransform.pivot = new Vector2(1f, 0f);
            loadingLabel.rectTransform.anchoredPosition = new Vector2(-56f, 42f);
            loadingLabel.rectTransform.sizeDelta = new Vector2(360f, 64f);
            loadingLabel.color = new Color(0.95f, 0.98f, 1f, 1f);
        }
    }

    static LoadingManager EnsureInstance()
    {
        if (instance != null)
        {
            return instance;
        }

        LoadingManager existing = Object.FindFirstObjectByType<LoadingManager>();
        if (existing != null)
        {
            instance = existing;
            return instance;
        }

        GameObject root = new("LoadingManager");
        instance = root.AddComponent<LoadingManager>();
        return instance;
    }

    static RectTransform CreateRectTransform(string name, Transform parent)
    {
        GameObject gameObject = new(name, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        return gameObject.GetComponent<RectTransform>();
    }

    static Image CreateImage(string name, Transform parent, Color color)
    {
        RectTransform rect = CreateRectTransform(name, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        return image;
    }

    static TextMeshProUGUI CreateText(string name, Transform parent, string value, float fontSize, FontStyles fontStyle, TextAlignmentOptions alignment, TMP_FontAsset font)
    {
        RectTransform rect = CreateRectTransform(name, parent);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;

        if (font != null)
        {
            text.font = font;
        }

        return text;
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }
}
