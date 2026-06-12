using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ExhaustionDeathManager : MonoBehaviour
{
    private const string DeathMessage = "bạn đã chết vì kiệt sức";
    private const string DefaultGameplaySceneName = "InGame";

    private static ExhaustionDeathManager instance;

    [Header("Overlay")]
    [SerializeField] private GameObject overlayRoot;
    [SerializeField] private CanvasGroup overlayCanvasGroup;
    [SerializeField] private TMP_Text messageLabel;
    [SerializeField] private Color backdropColor = new Color(0f, 0f, 0f, 0.92f);

    [Header("Timing")]
    [SerializeField, Min(0f)] private float fadeInDuration = 0.25f;
    [SerializeField, Min(0f)] private float messageVisibleDuration = 1.6f;

    private Coroutine deathRoutine;

    private void Awake()
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

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    public static void TriggerExhaustionDeath()
    {
        ExhaustionDeathManager manager = EnsureInstance();
        if (manager != null)
        {
            manager.BeginDeathSequence();
        }
    }

    private void BeginDeathSequence()
    {
        if (deathRoutine != null)
        {
            return;
        }

        deathRoutine = StartCoroutine(DeathSequenceRoutine());
    }

    private IEnumerator DeathSequenceRoutine()
    {
        EnsureFallbackUi();
        ShowOverlay();

        Time.timeScale = 0f;
        Input.ResetInputAxes();

        yield return FadeOverlayRoutine(1f, fadeInDuration);
        yield return new WaitForSecondsRealtime(messageVisibleDuration);

        ExhaustionDayResetService.ResetToStartOfCurrentDay();

        Time.timeScale = 1f;
        string sceneName = ResolveSceneToReload();
        deathRoutine = null;

        if (LoadingManager.LoadScene(sceneName))
        {
            HideOverlayImmediate();
            yield break;
        }

        SceneManager.LoadScene(sceneName);
        HideOverlayImmediate();
    }

    private string ResolveSceneToReload()
    {
        string activeSceneName = SceneManager.GetActiveScene().name;
        if (!string.IsNullOrWhiteSpace(activeSceneName) &&
            Application.CanStreamedLevelBeLoaded(activeSceneName))
        {
            return activeSceneName;
        }

        return DefaultGameplaySceneName;
    }

    private IEnumerator FadeOverlayRoutine(float targetAlpha, float duration)
    {
        if (overlayCanvasGroup == null)
        {
            yield break;
        }

        float startAlpha = overlayCanvasGroup.alpha;
        duration = Mathf.Max(0.01f, duration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            overlayCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            yield return null;
        }

        overlayCanvasGroup.alpha = targetAlpha;
    }

    private void ShowOverlay()
    {
        EnsureFallbackUi();

        if (messageLabel != null)
        {
            messageLabel.text = DeathMessage;
        }

        if (overlayRoot != null)
        {
            overlayRoot.SetActive(true);
            overlayRoot.transform.SetAsLastSibling();
        }

        if (overlayCanvasGroup != null)
        {
            overlayCanvasGroup.alpha = 0f;
            overlayCanvasGroup.interactable = false;
            overlayCanvasGroup.blocksRaycasts = true;
        }
    }

    private void HideOverlayImmediate()
    {
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

    private void EnsureFallbackUi()
    {
        if (overlayRoot != null && overlayCanvasGroup != null && messageLabel != null)
        {
            return;
        }

        BuildFallbackUi();
    }

    private void BuildFallbackUi()
    {
        TMP_FontAsset font = TMP_Settings.defaultFontAsset ??
            Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        if (overlayRoot == null || overlayCanvasGroup == null)
        {
            GameObject canvasObject = new GameObject(
                "ExhaustionDeathCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(CanvasGroup));

            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 6000;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            overlayRoot = canvasObject;
            overlayCanvasGroup = canvasObject.GetComponent<CanvasGroup>();

            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            Stretch(canvasRect);

            Image backdrop = CreateImage("Backdrop", canvasRect, backdropColor);
            Stretch(backdrop.rectTransform);
        }

        if (messageLabel == null)
        {
            RectTransform parent = overlayRoot.GetComponent<RectTransform>();
            messageLabel = CreateText("ExhaustionDeathLabel", parent, DeathMessage, 54f, font);
            messageLabel.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            messageLabel.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            messageLabel.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            messageLabel.rectTransform.anchoredPosition = Vector2.zero;
            messageLabel.rectTransform.sizeDelta = new Vector2(1100f, 180f);
        }
    }

    private static ExhaustionDeathManager EnsureInstance()
    {
        if (instance != null)
        {
            return instance;
        }

        ExhaustionDeathManager existing = FindFirstObjectByType<ExhaustionDeathManager>();
        if (existing != null)
        {
            instance = existing;
            return instance;
        }

        GameObject root = new GameObject("ExhaustionDeathManager");
        instance = root.AddComponent<ExhaustionDeathManager>();
        return instance;
    }

    private static Image CreateImage(string name, Transform parent, Color color)
    {
        RectTransform rect = CreateRectTransform(name, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        return image;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, string value, float fontSize, TMP_FontAsset font)
    {
        RectTransform rect = CreateRectTransform(name, parent);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;

        if (font != null)
        {
            text.font = font;
        }

        return text;
    }

    private static RectTransform CreateRectTransform(string name, Transform parent)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        return gameObject.GetComponent<RectTransform>();
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }
}
