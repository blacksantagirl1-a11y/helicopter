using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public class DialogueController : MonoBehaviour
{
    private sealed class DialogueRequest
    {
        public DialogueRequest(DialogueDay day, DialogueEventId eventId, DialogueEntry entry)
        {
            Day = day;
            EventId = eventId;
            Entry = entry;
        }

        public DialogueDay Day { get; }
        public DialogueEventId EventId { get; }
        public DialogueEntry Entry { get; }
    }

    private const string DefaultDatabaseResourcePath = "Dialogue/DialogueDatabase";
    private const float DefaultCharactersPerSecond = 48f;

    private static DialogueController instance;
    private static bool isCreatingInstance;

    [Header("Data")]
    [SerializeField] private DialogueDatabase database;
    [SerializeField] private string databaseResourcePath = DefaultDatabaseResourcePath;
    [SerializeField]
    [Min(1f)]
    private float charactersPerSecond = DefaultCharactersPerSecond;

    [Header("References")]
    [SerializeField] private PlayerUI playerUI;
    [SerializeField] private InventoryUIController inventoryUIController;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Jump jump;
    [SerializeField] private Crouch crouch;
    [SerializeField] private ActionScript actionScript;
    [SerializeField] private Rigidbody playerRigidbody;
    [SerializeField] private PlayerLook playerLook;
    [SerializeField] private Zoom zoom;
    [SerializeField] private PickUpScript pickUpScript;

    [Header("Dialogue UI")]
    [SerializeField] private GameObject dialogueRoot;
    [SerializeField] private Image dialogueBackground;
    [SerializeField] private TextMeshProUGUI speakerText;
    [SerializeField] private TextMeshProUGUI bodyText;

    private readonly Queue<DialogueRequest> pendingRequests = new Queue<DialogueRequest>();
    private readonly Dictionary<Behaviour, bool> cachedControlStates = new Dictionary<Behaviour, bool>();

    private DialogueRequest currentRequest;
    private bool isDialogueActive;
    private bool isTyping;
    private bool ignoreAdvanceUntilMouseReleased;
    private float currentVisibleCharacters;
    private int currentLineCharacterCount;
    private int currentLineIndex = -1;
    private float cachedTimeScale = 1f;
    private bool hasCachedTimeScale;

    public static bool IsDialogueActive => instance != null && instance.isDialogueActive;

    public static bool RequestDialogue(DialogueEventId eventId)
    {
        if (eventId == DialogueEventId.None)
        {
            Debug.LogWarning("DialogueController ignored a request for DialogueEventId.None.");
            return false;
        }

        DialogueController controller = EnsureInstance();
        if (controller == null)
        {
            Debug.LogWarning($"DialogueController could not be created for event '{eventId}'.");
            return false;
        }

        if (!controller.enabled)
        {
            controller.enabled = true;
        }

        return controller.EnqueueRequest(eventId);
    }

    public static DialogueDay GetCurrentDay()
    {
        return DialogueSaveService.GetCurrentDay();
    }

    public static void SetCurrentDay(DialogueDay day)
    {
        DialogueSaveService.SetCurrentDay(day);
    }

    public static DialogueDay AdvanceDay()
    {
        return DialogueSaveService.AdvanceDay();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        instance = null;
        isCreatingInstance = false;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        charactersPerSecond = Mathf.Max(1f, charactersPerSecond);
        ResolveDatabase();
        ResolveReferences();
        EnsureDialogueUI();
        SetDialogueVisible(false);
    }

    private void OnDisable()
    {
        RestoreDialogueState();
    }

    private void OnDestroy()
    {
        RestoreDialogueState();

        if (instance == this)
        {
            instance = null;
        }
    }

    private void OnValidate()
    {
        charactersPerSecond = Mathf.Max(1f, charactersPerSecond);
        ResolveDatabase();
        ResolveReferences();
    }

    private void Update()
    {
        if (!isDialogueActive)
        {
            TryStartNextDialogue();
            return;
        }

        if (ignoreAdvanceUntilMouseReleased)
        {
            if (Input.GetMouseButton(0))
            {
                UpdateTypewriter();
                return;
            }

            ignoreAdvanceUntilMouseReleased = false;
        }

        UpdateTypewriter();

        if (Input.GetMouseButtonDown(0))
        {
            HandleAdvanceInput();
        }
    }

    private static bool TryGetExistingInstance(out DialogueController controller)
    {
        if (instance != null)
        {
            controller = instance;
            return true;
        }

        controller = Object.FindFirstObjectByType<DialogueController>();
        if (controller != null)
        {
            instance = controller;
            return true;
        }

        return false;
    }

    private static DialogueController EnsureInstance()
    {
        if (TryGetExistingInstance(out DialogueController controller))
        {
            return controller;
        }

        if (isCreatingInstance)
        {
            return null;
        }

        isCreatingInstance = true;

        GameObject controllerObject = new GameObject("DialogueController");
        controller = controllerObject.AddComponent<DialogueController>();

        isCreatingInstance = false;
        return controller;
    }

    private bool EnqueueRequest(DialogueEventId eventId)
    {
        ResolveDatabase();
        if (database == null)
        {
            Debug.LogWarning(
                $"DialogueController could not find a DialogueDatabase. Expected a resource at '{databaseResourcePath}'.",
                this);
            return false;
        }

        DialogueDay currentDay = DialogueSaveService.GetCurrentDay();
        if (!database.TryGetEntry(currentDay, eventId, out DialogueEntry entry) || entry == null)
        {
            Debug.LogWarning(
                $"DialogueController could not find dialogue data for {currentDay} / {eventId}.",
                this);
            return false;
        }

        if (entry.LineCount < 1)
        {
            Debug.LogWarning(
                $"DialogueController ignored {currentDay} / {eventId} because it does not contain any dialogue lines.",
                this);
            return false;
        }

        pendingRequests.Enqueue(new DialogueRequest(currentDay, eventId, entry));
        TryStartNextDialogue();
        return true;
    }

    private void TryStartNextDialogue()
    {
        if (isDialogueActive || pendingRequests.Count == 0)
        {
            return;
        }

        ResolveReferences();
        EnsureDialogueUI();
        if (dialogueRoot == null || bodyText == null)
        {
            return;
        }

        while (pendingRequests.Count > 0)
        {
            DialogueRequest nextRequest = pendingRequests.Peek();
            if (nextRequest == null || nextRequest.Entry == null || nextRequest.Entry.LineCount < 1)
            {
                pendingRequests.Dequeue();
                continue;
            }

            pendingRequests.Dequeue();
            BeginDialogue(nextRequest);
            return;
        }
    }

    private void BeginDialogue(DialogueRequest request)
    {
        currentRequest = request;
        currentLineIndex = -1;
        currentVisibleCharacters = 0f;
        currentLineCharacterCount = 0;
        isTyping = false;
        isDialogueActive = true;
        ignoreAdvanceUntilMouseReleased = Input.GetMouseButton(0);

        ResolveReferences();
        EnsureDialogueUI();
        CacheTimeScale();
        ApplyTimeScale(request.Entry.TimeScale);
        PrepareGameplayForDialogue(request.Entry.PlayerCanMove);
        SetDialogueVisible(true);
        ShowLine(0);
    }

    private void HandleAdvanceInput()
    {
        if (!isDialogueActive || currentRequest == null)
        {
            return;
        }

        if (isTyping)
        {
            RevealCurrentLineImmediately();
            return;
        }

        int nextLineIndex = currentLineIndex + 1;
        if (currentRequest.Entry.TryGetLine(nextLineIndex, out _))
        {
            ShowLine(nextLineIndex);
            return;
        }

        EndCurrentDialogue();
    }

    private void ShowLine(int lineIndex)
    {
        if (currentRequest == null || !currentRequest.Entry.TryGetLine(lineIndex, out DialogueLineData line) || line == null)
        {
            EndCurrentDialogue();
            return;
        }

        currentLineIndex = lineIndex;
        currentVisibleCharacters = 0f;

        if (speakerText != null)
        {
            bool hasSpeaker = !string.IsNullOrWhiteSpace(line.SpeakerName);
            speakerText.gameObject.SetActive(hasSpeaker);
            speakerText.text = hasSpeaker ? line.SpeakerName : string.Empty;
        }

        if (bodyText == null)
        {
            EndCurrentDialogue();
            return;
        }

        bodyText.text = line.Text ?? string.Empty;
        bodyText.maxVisibleCharacters = 0;
        bodyText.ForceMeshUpdate();

        currentLineCharacterCount = bodyText.textInfo.characterCount;
        isTyping = currentLineCharacterCount > 0;

        if (!isTyping)
        {
            bodyText.maxVisibleCharacters = currentLineCharacterCount;
        }
    }

    private void UpdateTypewriter()
    {
        if (!isTyping || bodyText == null)
        {
            return;
        }

        currentVisibleCharacters += Mathf.Max(1f, charactersPerSecond) * Time.unscaledDeltaTime;
        int visibleCharacters = Mathf.Clamp(Mathf.FloorToInt(currentVisibleCharacters), 0, currentLineCharacterCount);
        bodyText.maxVisibleCharacters = visibleCharacters;

        if (visibleCharacters >= currentLineCharacterCount)
        {
            isTyping = false;
        }
    }

    private void RevealCurrentLineImmediately()
    {
        if (bodyText == null)
        {
            return;
        }

        currentVisibleCharacters = currentLineCharacterCount;
        bodyText.maxVisibleCharacters = currentLineCharacterCount;
        isTyping = false;
    }

    private void EndCurrentDialogue()
    {
        SetDialogueVisible(false);
        RestoreDialogueState();
        TryStartNextDialogue();
    }

    private void RestoreDialogueState()
    {
        RestoreTimeScale();
        RestoreGameplayState();

        currentRequest = null;
        isDialogueActive = false;
        isTyping = false;
        ignoreAdvanceUntilMouseReleased = false;
        currentVisibleCharacters = 0f;
        currentLineCharacterCount = 0;
        currentLineIndex = -1;

        if (speakerText != null)
        {
            speakerText.text = string.Empty;
        }

        if (bodyText != null)
        {
            bodyText.text = string.Empty;
            bodyText.maxVisibleCharacters = 0;
        }
    }

    private void PrepareGameplayForDialogue(bool playerCanMove)
    {
        ResolveReferences();

        if (playerUI != null)
        {
            playerUI.UpdatePrompt(string.Empty);
            playerUI.HideInteractionContent();
        }

        if (inventoryUIController != null && inventoryUIController.IsInventoryOpen)
        {
            inventoryUIController.SetInventoryOpen(false);
        }

        CacheAndDisableControl(inventoryUIController);
        CacheAndDisableControl(pickUpScript);
        CacheAndDisableControl(actionScript);

        if (!playerCanMove)
        {
            CacheAndDisableControl(playerMovement);
            CacheAndDisableControl(jump);
            CacheAndDisableControl(crouch);
            CacheAndDisableControl(playerLook);
            CacheAndDisableControl(zoom);
            ClearPlayerMotion();
        }
    }

    private void RestoreGameplayState()
    {
        if (cachedControlStates.Count == 0)
        {
            return;
        }

        foreach (KeyValuePair<Behaviour, bool> pair in cachedControlStates)
        {
            if (pair.Key != null)
            {
                pair.Key.enabled = pair.Value;
            }
        }

        cachedControlStates.Clear();
    }

    private void CacheAndDisableControl(Behaviour behaviour)
    {
        if (behaviour == null)
        {
            return;
        }

        if (!cachedControlStates.ContainsKey(behaviour))
        {
            cachedControlStates.Add(behaviour, behaviour.enabled);
        }

        behaviour.enabled = false;
    }

    private void ClearPlayerMotion()
    {
        if (playerRigidbody == null)
        {
            return;
        }

        playerRigidbody.linearVelocity = Vector3.zero;
        playerRigidbody.angularVelocity = Vector3.zero;
    }

    private void CacheTimeScale()
    {
        if (hasCachedTimeScale)
        {
            return;
        }

        cachedTimeScale = Time.timeScale;
        hasCachedTimeScale = true;
    }

    private void ApplyTimeScale(float targetTimeScale)
    {
        Time.timeScale = Mathf.Max(0f, targetTimeScale);
    }

    private void RestoreTimeScale()
    {
        if (!hasCachedTimeScale)
        {
            return;
        }

        Time.timeScale = cachedTimeScale;
        hasCachedTimeScale = false;
    }

    private void ResolveDatabase()
    {
        if (database != null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(databaseResourcePath))
        {
            database = Resources.Load<DialogueDatabase>(databaseResourcePath);
        }

        if (database == null)
        {
            DialogueDatabase[] databases = Resources.LoadAll<DialogueDatabase>(string.Empty);
            if (databases != null && databases.Length > 0)
            {
                database = databases[0];
            }
        }

#if UNITY_EDITOR
        if (database == null && !Application.isPlaying)
        {
            string[] guids = AssetDatabase.FindAssets("t:DialogueDatabase");
            if (guids != null && guids.Length > 0)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                database = AssetDatabase.LoadAssetAtPath<DialogueDatabase>(assetPath);
            }
        }
#endif
    }

    private void ResolveReferences()
    {
        playerUI ??= FindFirstObjectByType<PlayerUI>();
        inventoryUIController ??= FindFirstObjectByType<InventoryUIController>();
        playerMovement ??= FindFirstObjectByType<PlayerMovement>();

        if (playerMovement != null)
        {
            jump ??= playerMovement.GetComponent<Jump>();
            crouch ??= playerMovement.GetComponent<Crouch>();
            actionScript ??= playerMovement.GetComponent<ActionScript>();
            playerRigidbody ??= playerMovement.GetComponent<Rigidbody>();
        }

        Camera referenceCamera = Camera.main;
        if (referenceCamera == null && playerMovement != null)
        {
            referenceCamera = playerMovement.GetComponentInChildren<Camera>(true);
        }

        if (referenceCamera != null)
        {
            playerLook ??= referenceCamera.GetComponent<PlayerLook>();
            zoom ??= referenceCamera.GetComponent<Zoom>();
            pickUpScript ??= referenceCamera.GetComponent<PickUpScript>();
        }

        jump ??= FindFirstObjectByType<Jump>();
        crouch ??= FindFirstObjectByType<Crouch>();
        actionScript ??= FindFirstObjectByType<ActionScript>();
        playerRigidbody ??= FindFirstObjectByType<Rigidbody>();
        playerLook ??= FindFirstObjectByType<PlayerLook>();
        zoom ??= FindFirstObjectByType<Zoom>();
        pickUpScript ??= FindFirstObjectByType<PickUpScript>();
    }

    private void EnsureDialogueUI()
    {
        if (dialogueRoot != null && dialogueBackground != null && speakerText != null && bodyText != null)
        {
            return;
        }

        Canvas canvas = ResolveCanvas();
        if (canvas == null)
        {
            return;
        }

        if (dialogueRoot == null)
        {
            Transform existingRoot = canvas.transform.Find("DialogueRoot");
            if (existingRoot != null)
            {
                dialogueRoot = existingRoot.gameObject;
            }
        }

        if (dialogueRoot == null)
        {
            dialogueRoot = new GameObject("DialogueRoot", typeof(RectTransform), typeof(Image));
            dialogueRoot.transform.SetParent(canvas.transform, false);
        }

        dialogueBackground ??= dialogueRoot.GetComponent<Image>();
        if (dialogueBackground == null)
        {
            dialogueBackground = dialogueRoot.AddComponent<Image>();
        }

        if (speakerText == null)
        {
            Transform existingSpeaker = dialogueRoot.transform.Find("SpeakerText");
            if (existingSpeaker != null)
            {
                speakerText = existingSpeaker.GetComponent<TextMeshProUGUI>();
            }
        }

        if (bodyText == null)
        {
            Transform existingBody = dialogueRoot.transform.Find("BodyText");
            if (existingBody != null)
            {
                bodyText = existingBody.GetComponent<TextMeshProUGUI>();
            }
        }

        speakerText ??= CreateTextElement("SpeakerText", dialogueRoot.transform);
        bodyText ??= CreateTextElement("BodyText", dialogueRoot.transform);

        ConfigureDialogueRoot();
        ConfigureSpeakerText();
        ConfigureBodyText();
    }

    private Canvas ResolveCanvas()
    {
        if (playerUI != null && playerUI.PickUpText != null && playerUI.PickUpText.canvas != null)
        {
            return playerUI.PickUpText.canvas;
        }

        return FindFirstObjectByType<Canvas>();
    }

    private void ConfigureDialogueRoot()
    {
        if (dialogueRoot == null || dialogueBackground == null)
        {
            return;
        }

        RectTransform rootRect = dialogueRoot.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0f);
        rootRect.anchorMax = new Vector2(0.5f, 0f);
        rootRect.pivot = new Vector2(0.5f, 0f);
        rootRect.anchoredPosition = new Vector2(0f, 32f);
        rootRect.sizeDelta = new Vector2(980f, 190f);

        dialogueBackground.color = new Color(0f, 0f, 0f, 0.72f);
        dialogueBackground.raycastTarget = false;
    }

    private void ConfigureSpeakerText()
    {
        if (speakerText == null)
        {
            return;
        }

        RectTransform textRect = speakerText.rectTransform;
        textRect.anchorMin = new Vector2(0f, 1f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.pivot = new Vector2(0.5f, 1f);
        textRect.offsetMin = new Vector2(32f, -44f);
        textRect.offsetMax = new Vector2(-32f, -10f);

        speakerText.text = string.Empty;
        speakerText.color = new Color(0.93f, 0.75f, 0.34f, 1f);
        speakerText.alignment = TextAlignmentOptions.TopLeft;
        speakerText.enableWordWrapping = false;
        speakerText.fontSize = 30f;
        speakerText.raycastTarget = false;
    }

    private void ConfigureBodyText()
    {
        if (bodyText == null)
        {
            return;
        }

        RectTransform textRect = bodyText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.offsetMin = new Vector2(32f, 20f);
        textRect.offsetMax = new Vector2(-32f, -56f);

        bodyText.text = string.Empty;
        bodyText.color = Color.white;
        bodyText.alignment = TextAlignmentOptions.TopLeft;
        bodyText.enableWordWrapping = true;
        bodyText.fontSize = 34f;
        bodyText.lineSpacing = -4f;
        bodyText.raycastTarget = false;
    }

    private void SetDialogueVisible(bool visible)
    {
        if (visible)
        {
            EnsureDialogueUI();
        }

        if (dialogueRoot != null)
        {
            dialogueRoot.SetActive(visible);
        }
    }

    private static TextMeshProUGUI CreateTextElement(string objectName, Transform parent)
    {
        GameObject textObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = string.Empty;
        text.color = Color.white;
        return text;
    }
}
