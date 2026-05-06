using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class MiniGameCookingController : MonoBehaviour
{
    private const string RootName = "MiniGameCooking";
    private const string BackgroundName = "BackgroundCooking";
    private const string RectangleName = "RectangleCooking";
    private const string SliderName = "SliderCooking";
    private const string WinName = "WinCooking";
    private const string LoseName = "LoseCooking";

    [Header("References")]
    [SerializeField] private RectTransform backgroundCooking;
    [SerializeField] private RectTransform rectangleCooking;
    [SerializeField] private RectTransform sliderCooking;
    [SerializeField] private GameObject winCooking;
    [SerializeField] private GameObject loseCooking;

    [Header("Timing")]
    [SerializeField] private float sliderSpeed = 420f;
    [SerializeField] private float resultDuration = 2f;

    private float sliderMinX;
    private float sliderMaxX;
    private float sliderCurrentX;
    private float sliderDirection = 1f;
    private bool isRunning;
    private Coroutine finishRoutine;
    private CampingCookingInteractable cookingTarget;
    private CampingCookingModeController cookingModeController;
    private AudioSource loopingCookingSource;
    private bool loopingCookingSourceWasLooping;

    public bool IsActive => gameObject.activeInHierarchy && (isRunning || finishRoutine != null);

    private void Awake()
    {
        TryAutoAssignReferences();
        HideResultLabels();
    }

    private void OnDisable()
    {
        StopCookingSoundLoop();
        isRunning = false;
        if (finishRoutine != null)
        {
            StopCoroutine(finishRoutine);
            finishRoutine = null;
        }
    }

    private void Update()
    {
        if (!isRunning)
        {
            return;
        }

        MoveSlider();

        if (Input.GetMouseButtonDown(0))
        {
            Resolve(IsSliderTouchingRectangle());
        }
    }

    public static bool TryOpen(
        GameObject miniGamePrefab,
        Transform fallbackParent,
        CampingCookingInteractable target,
        CampingCookingModeController modeController)
    {
        MiniGameCookingController controller = FindExistingController();
        if (controller == null)
        {
            GameObject existingRoot = FindObjectByName(RootName);
            if (existingRoot != null)
            {
                controller = existingRoot.GetComponent<MiniGameCookingController>();
                if (controller == null)
                {
                    controller = existingRoot.AddComponent<MiniGameCookingController>();
                }
            }
        }

        if (controller == null && miniGamePrefab != null)
        {
            GameObject instance = Instantiate(miniGamePrefab, fallbackParent);
            instance.name = miniGamePrefab.name;
            controller = instance.GetComponent<MiniGameCookingController>();
            if (controller == null)
            {
                controller = instance.AddComponent<MiniGameCookingController>();
            }
        }

        return controller != null && controller.Begin(target, modeController);
    }

    public static bool IsAnyMiniGameActive()
    {
        MiniGameCookingController[] controllers = FindObjectsByType<MiniGameCookingController>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < controllers.Length; i++)
        {
            if (controllers[i] != null && controllers[i].IsActive)
            {
                return true;
            }
        }

        return false;
    }

    public static void CloseAny()
    {
        MiniGameCookingController[] controllers = FindObjectsByType<MiniGameCookingController>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < controllers.Length; i++)
        {
            if (controllers[i] != null)
            {
                controllers[i].Close();
            }
        }
    }

    public bool Begin(CampingCookingInteractable target, CampingCookingModeController modeController)
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        TryAutoAssignReferences();
        if (backgroundCooking == null || rectangleCooking == null || sliderCooking == null)
        {
            Debug.LogWarning("MiniGameCooking thieu BackgroundCooking, RectangleCooking hoac SliderCooking.");
            gameObject.SetActive(false);
            return false;
        }

        if (finishRoutine != null)
        {
            StopCoroutine(finishRoutine);
            finishRoutine = null;
        }

        HideResultLabels();
        PlaceRectangleRandomly();
        ResetSlider();
        cookingTarget = target;
        cookingModeController = modeController;
        isRunning = true;
        StartCookingSoundLoop();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        return true;
    }

    public void Close()
    {
        StopCookingSoundLoop();
        isRunning = false;
        HideResultLabels();
        cookingTarget = null;
        cookingModeController = null;
        gameObject.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private static MiniGameCookingController FindExistingController()
    {
        MiniGameCookingController[] controllers = FindObjectsByType<MiniGameCookingController>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        return controllers.Length > 0 ? controllers[0] : null;
    }

    private static GameObject FindObjectByName(string objectName)
    {
        Transform[] transforms = FindObjectsByType<Transform>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i] != null && transforms[i].name == objectName)
            {
                return transforms[i].gameObject;
            }
        }

        return null;
    }

    private void TryAutoAssignReferences()
    {
        backgroundCooking ??= FindChildRect(BackgroundName);
        rectangleCooking ??= FindChildRect(RectangleName);
        sliderCooking ??= FindChildRect(SliderName);
        winCooking ??= FindChildObject(WinName);
        loseCooking ??= FindChildObject(LoseName);
    }

    private RectTransform FindChildRect(string childName)
    {
        GameObject child = FindChildObject(childName);
        return child != null ? child.GetComponent<RectTransform>() : null;
    }

    private GameObject FindChildObject(string childName)
    {
        Transform[] transforms = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i] != null && transforms[i].name == childName)
            {
                return transforms[i].gameObject;
            }
        }

        return null;
    }

    private void HideResultLabels()
    {
        if (winCooking != null)
        {
            winCooking.SetActive(false);
        }

        if (loseCooking != null)
        {
            loseCooking.SetActive(false);
        }
    }

    private void PlaceRectangleRandomly()
    {
        float maxX = Mathf.Max(0f, (backgroundCooking.rect.width - rectangleCooking.rect.width) * 0.5f);
        float maxY = Mathf.Max(0f, (backgroundCooking.rect.height - rectangleCooking.rect.height) * 0.5f);

        rectangleCooking.anchoredPosition = new Vector2(
            Random.Range(-maxX, maxX),
            maxY > 0f ? Random.Range(-maxY, maxY) : 0f);
    }

    private void ResetSlider()
    {
        float halfTravel = Mathf.Max(0f, (backgroundCooking.rect.width - sliderCooking.rect.width) * 0.5f);
        sliderMinX = backgroundCooking.anchoredPosition.x - halfTravel;
        sliderMaxX = backgroundCooking.anchoredPosition.x + halfTravel;
        sliderCurrentX = sliderMinX;
        sliderDirection = 1f;
        sliderCooking.anchoredPosition = new Vector2(sliderCurrentX, backgroundCooking.anchoredPosition.y);
    }

    private void MoveSlider()
    {
        sliderCurrentX += sliderDirection * sliderSpeed * Time.unscaledDeltaTime;
        if (sliderCurrentX >= sliderMaxX)
        {
            sliderCurrentX = sliderMaxX;
            sliderDirection = -1f;
        }
        else if (sliderCurrentX <= sliderMinX)
        {
            sliderCurrentX = sliderMinX;
            sliderDirection = 1f;
        }

        sliderCooking.anchoredPosition = new Vector2(sliderCurrentX, backgroundCooking.anchoredPosition.y);
    }

    private void Resolve(bool isWin)
    {
        isRunning = false;
        if (isWin && cookingTarget != null)
        {
            cookingTarget.AddCookedFood();
        }

        if (winCooking != null)
        {
            winCooking.SetActive(isWin);
        }

        if (loseCooking != null)
        {
            loseCooking.SetActive(!isWin);
        }

        finishRoutine = StartCoroutine(CloseAfterResult());
    }

    private void StartCookingSoundLoop()
    {
        SoundManager soundManager = ResolveSoundManager();
        if (soundManager == null || soundManager.cookingSource == null)
        {
            return;
        }

        AudioSource cookingSource = soundManager.cookingSource;
        if (loopingCookingSource != cookingSource)
        {
            StopCookingSoundLoop();
            loopingCookingSource = cookingSource;
            loopingCookingSourceWasLooping = cookingSource.loop;
        }

        cookingSource.loop = true;
        if (!cookingSource.isPlaying)
        {
            cookingSource.Play();
        }
    }

    private void StopCookingSoundLoop()
    {
        if (loopingCookingSource == null)
        {
            return;
        }

        loopingCookingSource.Stop();
        loopingCookingSource.loop = loopingCookingSourceWasLooping;
        loopingCookingSource = null;
    }

    private static SoundManager ResolveSoundManager()
    {
        return SoundManager.Instance != null
            ? SoundManager.Instance
            : FindFirstObjectByType<SoundManager>();
    }

    private IEnumerator CloseAfterResult()
    {
        yield return new WaitForSecondsRealtime(resultDuration);
        finishRoutine = null;
        CampingCookingModeController modeController = cookingModeController;
        Close();

        if (modeController != null && modeController.IsCookingModeActive)
        {
            modeController.ExitCookingMode();
        }
    }

    private bool IsSliderTouchingRectangle()
    {
        return GetWorldRect(sliderCooking).Overlaps(GetWorldRect(rectangleCooking), true);
    }

    private static Rect GetWorldRect(RectTransform rectTransform)
    {
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);

        float minX = corners[0].x;
        float maxX = corners[0].x;
        float minY = corners[0].y;
        float maxY = corners[0].y;

        for (int i = 1; i < corners.Length; i++)
        {
            minX = Mathf.Min(minX, corners[i].x);
            maxX = Mathf.Max(maxX, corners[i].x);
            minY = Mathf.Min(minY, corners[i].y);
            maxY = Mathf.Max(maxY, corners[i].y);
        }

        return Rect.MinMaxRect(minX, minY, maxX, maxY);
    }
}
