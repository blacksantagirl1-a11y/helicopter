using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class Day3HintSequenceController : MonoBehaviour
{
    private const string HostObjectName = "LakeTriggerDay3";
    private const string HintObjectName = "HintDay3";
    private const string HintTemplateObjectName = "HintDay4";
    private const string HintUnlockedKey = "day3Hint.unlocked";
    private const string HintCompletedKey = "day3Hint.completed";
    private const string HintLastKnownDayKey = "day3Hint.lastKnownDay";

    private static bool installerRegistered;

    [SerializeField] private DialogueDay requiredDay = DialogueDay.Day3;
    [SerializeField] private DialogueEventId unlockDialogueEvent = DialogueEventId.InvestigationStart;
    [SerializeField] private DialogueEventId hintDialogueEvent = DialogueEventId.InvestigationProgress;
    [SerializeField] private string hintObjectName = HintObjectName;
    [SerializeField] private string hintTemplateObjectName = HintTemplateObjectName;
    [SerializeField] private Vector3 hintWorldOffset = new Vector3(0f, 2.75f, 0f);

    private GameObject hintObject;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetInstallerState()
    {
        installerRegistered = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InstallAfterSceneLoad()
    {
        RegisterSceneCallbacks();
        TryInstallInLoadedScenes();
    }

    private void Awake()
    {
        SyncPersistentState();
        hintObject = FindSceneObjectByName(hintObjectName, gameObject.scene, true);
        if (hintObject != null)
        {
            EnsureHintInteractable(hintObject);
        }
    }

    private void OnEnable()
    {
        SyncPersistentState();
        DialogueController.DialogueFinished += HandleDialogueFinished;
        DialogueSaveService.CurrentDayChanged += HandleCurrentDayChanged;
        RefreshHintState();
    }

    private void OnDisable()
    {
        DialogueController.DialogueFinished -= HandleDialogueFinished;
        DialogueSaveService.CurrentDayChanged -= HandleCurrentDayChanged;
    }

    public static bool IsHintCompleted()
    {
        SyncPersistentState();
        return PlayerPrefs.GetInt(HintCompletedKey, 0) == 1;
    }

    public static void MarkHintCompleted()
    {
        SyncPersistentState();
        PlayerPrefs.SetInt(HintUnlockedKey, 1);
        PlayerPrefs.SetInt(HintCompletedKey, 1);
        PlayerPrefs.Save();
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
        TryInstallInScene(scene);
    }

    private static void TryInstallInLoadedScenes()
    {
        for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
        {
            TryInstallInScene(SceneManager.GetSceneAt(sceneIndex));
        }
    }

    private static void TryInstallInScene(Scene scene)
    {
        if (!scene.isLoaded)
        {
            return;
        }

        GameObject hostObject = FindSceneObjectByName(HostObjectName, scene, false);
        if (hostObject == null || hostObject.GetComponent<Day3HintSequenceController>() != null)
        {
            return;
        }

        hostObject.AddComponent<Day3HintSequenceController>();
    }

    private void HandleDialogueFinished(DialogueDay day, DialogueEventId eventId)
    {
        if (day != requiredDay || eventId != unlockDialogueEvent)
        {
            return;
        }

        UnlockHintForCurrentSequence();
        RefreshHintState();
    }

    private void HandleCurrentDayChanged(DialogueDay day)
    {
        SyncPersistentState();
        RefreshHintState();
    }

    private void RefreshHintState()
    {
        bool shouldShowHint =
            DialogueController.GetCurrentDay() == requiredDay &&
            IsHintUnlocked() &&
            !IsHintCompleted();

        if (!shouldShowHint)
        {
            if (hintObject != null)
            {
                hintObject.SetActive(false);
            }

            return;
        }

        GameObject targetHint = EnsureHintObject();
        if (targetHint != null)
        {
            targetHint.SetActive(true);
        }
    }

    private GameObject EnsureHintObject()
    {
        if (hintObject != null)
        {
            EnsureHintInteractable(hintObject);
            return hintObject;
        }

        hintObject = FindSceneObjectByName(hintObjectName, gameObject.scene, true);
        if (hintObject != null)
        {
            EnsureHintInteractable(hintObject);
            return hintObject;
        }

        GameObject templateObject = FindSceneObjectByName(hintTemplateObjectName, gameObject.scene, true);
        if (templateObject == null)
        {
            Debug.LogWarning($"Day3HintSequenceController could not find template object '{hintTemplateObjectName}'.", this);
            return null;
        }

        GameObject root = new GameObject(hintObjectName);
        root.layer = templateObject.layer;

        Transform rootTransform = root.transform;
        rootTransform.SetParent(transform.parent, true);
        rootTransform.position = transform.position + hintWorldOffset;
        rootTransform.rotation = templateObject.transform.rotation;
        SetWorldScale(rootTransform, templateObject.transform.lossyScale);

        GameObject visualClone = Instantiate(templateObject, rootTransform);
        visualClone.name = "HintDay3Visual";
        visualClone.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        visualClone.transform.localScale = Vector3.one;

        hintObject = root;
        EnsureHintInteractable(hintObject);
        hintObject.SetActive(false);
        return hintObject;
    }

    private void EnsureHintInteractable(GameObject targetHint)
    {
        if (targetHint == null)
        {
            return;
        }

        HintDay3Interactable interactable = targetHint.GetComponent<HintDay3Interactable>();
        if (interactable == null)
        {
            interactable = targetHint.AddComponent<HintDay3Interactable>();
        }

        interactable.Configure(requiredDay, hintDialogueEvent);
    }

    private static void SyncPersistentState()
    {
        int currentDay = Mathf.Max((int)DialogueDay.Day1, (int)DialogueController.GetCurrentDay());
        int lastKnownDay = PlayerPrefs.GetInt(HintLastKnownDayKey, currentDay);

        if (currentDay < (int)DialogueDay.Day3 && lastKnownDay >= (int)DialogueDay.Day3)
        {
            PlayerPrefs.DeleteKey(HintUnlockedKey);
            PlayerPrefs.DeleteKey(HintCompletedKey);
        }

        if (lastKnownDay != currentDay)
        {
            PlayerPrefs.SetInt(HintLastKnownDayKey, currentDay);
        }

        PlayerPrefs.Save();
    }

    private static bool IsHintUnlocked()
    {
        SyncPersistentState();
        return PlayerPrefs.GetInt(HintUnlockedKey, 0) == 1;
    }

    private static void UnlockHintForCurrentSequence()
    {
        SyncPersistentState();

        PlayerPrefs.SetInt(HintUnlockedKey, 1);
        PlayerPrefs.DeleteKey(HintCompletedKey);
        PlayerPrefs.Save();
    }

    private static GameObject FindSceneObjectByName(string objectName, Scene scene, bool includeInactive)
    {
        if (string.IsNullOrWhiteSpace(objectName) || !scene.isLoaded)
        {
            return null;
        }

        GameObject[] rootObjects = scene.GetRootGameObjects();
        for (int rootIndex = 0; rootIndex < rootObjects.Length; rootIndex++)
        {
            GameObject rootObject = rootObjects[rootIndex];
            if (rootObject == null)
            {
                continue;
            }

            Transform[] transforms = rootObject.GetComponentsInChildren<Transform>(includeInactive);
            for (int index = 0; index < transforms.Length; index++)
            {
                Transform candidate = transforms[index];
                if (candidate != null && candidate.name == objectName)
                {
                    return candidate.gameObject;
                }
            }
        }

        return null;
    }

    private static void SetWorldScale(Transform target, Vector3 desiredWorldScale)
    {
        if (target == null)
        {
            return;
        }

        Transform parent = target.parent;
        if (parent == null)
        {
            target.localScale = desiredWorldScale;
            return;
        }

        Vector3 parentScale = parent.lossyScale;
        target.localScale = new Vector3(
            SafeDivide(desiredWorldScale.x, parentScale.x),
            SafeDivide(desiredWorldScale.y, parentScale.y),
            SafeDivide(desiredWorldScale.z, parentScale.z));
    }

    private static float SafeDivide(float value, float divisor)
    {
        return Mathf.Abs(divisor) > 0.0001f ? value / divisor : value;
    }
}
