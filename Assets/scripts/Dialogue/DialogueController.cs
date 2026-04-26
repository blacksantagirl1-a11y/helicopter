using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
// Day la bo dieu phoi trung tam cua he thong hoi thoai.
// Cach no hoat dong, theo ngon ngu de hieu:
// 1. Mot script bat ky trong game goi RequestDialogue(eventId).
// 2. Controller xem "hom nay la ngay may" trong save.
// 3. Controller tim doan hoi thoai dung voi cap: Day + EventId.
// 4. Doan hoi thoai duoc dua vao hang doi, roi hien tung dong len UI.
// 5. Trong luc hoi thoai chay, mot so dieu khien cua nguoi choi se bi tam khoa.
// 6. Ket thuc hoi thoai thi UI bien mat va cac dieu khien duoc bat lai nhu cu.
public class DialogueController : MonoBehaviour
{
    // Lop nho nay la "phieu yeu cau" cho 1 lan hoi thoai.
    // No luu lai du lieu da tim thay de controller xu ly lan luot trong hang doi.
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

    // Neu khong keo tay trong Inspector thi controller se tu load database o duong dan nay.
    private const string DefaultDatabaseResourcePath = "Dialogue/DialogueDatabase";
    // Toc do mac dinh cua hieu ung "go chu" (typewriter).
    private const float DefaultCharactersPerSecond = 48f;

    // instance giup he thong dung theo kieu "chi co 1 controller dang hoat dong".
    private static DialogueController instance;
    private static bool isCreatingInstance;

    [Header("Data")]
    // Database la noi chua toan bo kich ban hoi thoai cua game.
    [SerializeField] private DialogueDatabase database;
    // Duong dan Resources de auto tim database neu o tren chua duoc gan tay.
    [SerializeField] private string databaseResourcePath = DefaultDatabaseResourcePath;
    [SerializeField]
    [Min(1f)]
    // So ky tu hien ra moi giay khi dong thoai dang duoc "go" dan ra.
    private float charactersPerSecond = DefaultCharactersPerSecond;

    [Header("References")]
    // Cac reference ben duoi la nhung thanh phan gameplay co the bi an / khoa
    // trong luc dang hien hoi thoai.
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
    [SerializeField] private GameObject dayBadgeRoot;
    [SerializeField] private Image dayBadgeBackground;
    [SerializeField] private TextMeshProUGUI dayText;
    [SerializeField] private TextMeshProUGUI speakerText;
    [SerializeField] private TextMeshProUGUI bodyText;
    [SerializeField] private TextMeshProUGUI advanceHintText;

    // Hang doi nay tranh truong hop nhieu noi trong game cung yeu cau hoi thoai mot luc.
    private readonly Queue<DialogueRequest> pendingRequests = new Queue<DialogueRequest>();
    // Truoc khi tat control cua player, ta nho lai trang thai cu de sau hoi thoai con tra lai dung.
    private readonly Dictionary<Behaviour, bool> cachedControlStates = new Dictionary<Behaviour, bool>();

    // currentRequest = doan hoi thoai dang chay ngay luc nay.
    private DialogueRequest currentRequest;
    private bool isDialogueActive;
    // isTyping = true khi dong hien tai van dang hien dan tung chu.
    private bool isTyping;
    // Co nay tranh viec nguoi choi dang giu chuot luc mo hoi thoai lam skip mat dong dau tien.
    private bool ignoreAdvanceUntilMouseReleased;
    private float currentVisibleCharacters;
    private int currentLineCharacterCount;
    private int currentLineIndex = -1;
    // TimeScale co the bi doi trong hoi thoai, nen can nho lai gia tri cu.
    private float cachedTimeScale = 1f;
    private bool hasCachedTimeScale;

    public static bool IsDialogueActive => instance != null && instance.isDialogueActive;

    // Day la "cua vao" don gian nhat de script khac bat dau hoi thoai.
    // Chi can dua vao EventId, controller se tu lo viec tim data theo ngay hien tai.
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

    // Cho phep script khac dat ngay hien tai cua he thong hoi thoai.
    public static void SetCurrentDay(DialogueDay day)
    {
        DialogueSaveService.SetCurrentDay(day);
    }

    // Tang ngay len 1 buoc va luu lai.
    public static DialogueDay AdvanceDay()
    {
        return DialogueSaveService.AdvanceDay();
    }

    // Moi lan vao Play Mode hoac load lai domain, can reset bien static
    // de tranh giu lai instance cu mot cach sai lech.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        instance = null;
        isCreatingInstance = false;
    }

    // Awake: chuan bi database, tim reference, tao UI neu can va dam bao hop hoi thoai dang an.
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

    // Neu component bi tat trong luc dialogue dang mo, ta van phai tra gameplay ve trang thai an toan.
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

    // OnValidate chay trong Editor de giu gia tri hop le va auto tim reference som.
    private void OnValidate()
    {
        charactersPerSecond = Mathf.Max(1f, charactersPerSecond);
        ResolveDatabase();
        ResolveReferences();
    }

    // Update la "vong lap" chay moi frame.
    // Neu chua co hoi thoai thi thu lay yeu cau tiep theo trong hang doi.
    // Neu dang co hoi thoai thi cap nhat hieu ung go chu va lang nghe click chuot de qua dong.
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

    // Thu tim instance da ton tai san trong scene.
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

    // Neu scene chua co DialogueController, ham nay se tu tao mot GameObject moi va gan script vao.
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

    // Buoc nay bien "toi muon mo hoi thoai X" thanh "toi da tim duoc du lieu hoi thoai cu the".
    // Sau do dua vao hang doi de xu ly lan luot.
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

    // Neu khong co hoi thoai nao dang chay thi bat dau hoi thoai ke tiep trong hang doi.
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

    // Thiet lap trang thai ban dau cho 1 doan hoi thoai moi.
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
        RefreshDayBadge(request.Day);
        SetDialogueVisible(true);
        ShowLine(0);
    }

    // Click chuot khi dang hoi thoai se co 2 nghia:
    // - Neu dong hien tai dang hien dan: hien ra het ngay lap tuc.
    // - Neu dong hien tai da hien xong: sang dong tiep theo, hoac ket thuc neu da het dong.
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

    // Nap 1 dong thoai len UI va reset hieu ung go chu cho dong moi.
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

    // Moi frame tang so ky tu duoc hien ra, tao cam giac text dang duoc go dan.
    // Dung unscaledDeltaTime de van chay dung ngay ca khi Time.timeScale dang bi giam.
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

    // Dung khi nguoi choi click de bo qua hieu ung go chu va hien het dong ngay lap tuc.
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

    // Dong hoi thoai hien tai, an UI va tra gameplay ve trang thai cu.
    private void EndCurrentDialogue()
    {
        DialogueRequest completedRequest = currentRequest;
        SetDialogueVisible(false);
        RestoreDialogueState();
        HandlePostDialogueEffects(completedRequest);
        TryStartNextDialogue();
    }

    private void HandlePostDialogueEffects(DialogueRequest completedRequest)
    {
        if (completedRequest == null)
        {
            return;
        }

        DailyQuestManager.NotifyDialogueFinished(completedRequest.Day, completedRequest.EventId);
        TryActivateQuestFromDialogue(completedRequest.Day, completedRequest.Entry);
    }

    private static void TryActivateQuestFromDialogue(DialogueDay day, DialogueEntry entry)
    {
        if (entry == null || entry.LineCount < 1)
        {
            return;
        }

        for (int lineIndex = 0; lineIndex < entry.LineCount; lineIndex++)
        {
            if (!entry.TryGetLine(lineIndex, out DialogueLineData line) ||
                line == null ||
                line.QuestAction != DialogueQuestAction.AssignDailyQuest ||
                line.QuestId == DailyQuestId.None)
            {
                continue;
            }

            DailyQuestManager.TryActivateQuest(day, line.QuestId);
            return;
        }
    }

    // Gom tat ca buoc "don dep" de dua game tro lai trang thai truoc hoi thoai.
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

        if (dayText != null)
        {
            dayText.text = string.Empty;
        }
    }

    // Chuan bi gameplay truoc khi mo hoi thoai:
    // - an prompt tuong tac
    // - dong inventory neu dang mo
    // - khoa nhung script de tranh nguoi choi vua hoi thoai vua thao tac linh tinh
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

    // Mo lai cac control theo dung trang thai da nho truoc do.
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

    // Nho xem component nay dang bat hay tat, roi tat di tam thoi.
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

    // Neu hoi thoai khoa di chuyen thi dat van toc ve 0 de player dung yen ngay.
    private void ClearPlayerMotion()
    {
        if (playerRigidbody == null)
        {
            return;
        }

        playerRigidbody.linearVelocity = Vector3.zero;
        playerRigidbody.angularVelocity = Vector3.zero;
    }

    // Luu lai toc do thoi gian hien tai cua game.
    private void CacheTimeScale()
    {
        if (hasCachedTimeScale)
        {
            return;
        }

        cachedTimeScale = Time.timeScale;
        hasCachedTimeScale = true;
    }

    // DialogueEntry co the yeu cau game chay cham lai, dung lai, hoac giu nguyen toc do.
    private void ApplyTimeScale(float targetTimeScale)
    {
        Time.timeScale = Mathf.Max(0f, targetTimeScale);
    }

    // Sau hoi thoai, tra Time.timeScale lai nhu cu.
    private void RestoreTimeScale()
    {
        if (!hasCachedTimeScale)
        {
            return;
        }

        Time.timeScale = cachedTimeScale;
        hasCachedTimeScale = false;
    }

    // Tim database theo thu tu uu tien:
    // 1. Database da gan san trong Inspector.
    // 2. File Resources theo duong dan cau hinh.
    // 3. Bat ky DialogueDatabase nao tim thay trong Resources.
    // 4. Trong Editor: tim asset bang AssetDatabase de de setup hon.
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

    // Tu dong tim cac component can thiet trong scene de tranh viec phai gan tay.
    // de tranh viec phai gan tay trong Inspector, giup giam loi sai va de test hon.
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

    // Dam bao UI hoi thoai ton tai.
    // Neu scene chua co san, script se tu tao ra mot khung don gian trong Canvas.
    private void EnsureDialogueUI()
    {
        if (dialogueRoot != null &&
            dialogueBackground != null &&
            dayBadgeRoot != null &&
            dayBadgeBackground != null &&
            dayText != null &&
            speakerText != null &&
            bodyText != null &&
            advanceHintText != null)
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

        if (dayBadgeRoot == null)
        {
            Transform existingDayBadge = dialogueRoot.transform.Find("DayBadge");
            if (existingDayBadge != null)
            {
                dayBadgeRoot = existingDayBadge.gameObject;
            }
        }

        if (dayBadgeRoot == null)
        {
            dayBadgeRoot = new GameObject("DayBadge", typeof(RectTransform), typeof(Image));
            dayBadgeRoot.transform.SetParent(dialogueRoot.transform, false);
        }

        dayBadgeBackground ??= dayBadgeRoot.GetComponent<Image>();
        if (dayBadgeBackground == null)
        {
            dayBadgeBackground = dayBadgeRoot.AddComponent<Image>();
        }

        if (speakerText == null)
        {
            Transform existingSpeaker = dialogueRoot.transform.Find("SpeakerText");
            if (existingSpeaker != null)
            {
                speakerText = existingSpeaker.GetComponent<TextMeshProUGUI>();
            }
        }

        if (dayText == null)
        {
            Transform existingDayText = dayBadgeRoot.transform.Find("DayText");
            if (existingDayText != null)
            {
                dayText = existingDayText.GetComponent<TextMeshProUGUI>();
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

        if (advanceHintText == null)
        {
            Transform existingAdvanceHint = dialogueRoot.transform.Find("AdvanceHintText");
            if (existingAdvanceHint != null)
            {
                advanceHintText = existingAdvanceHint.GetComponent<TextMeshProUGUI>();
            }
        }

        dayText ??= CreateTextElement("DayText", dayBadgeRoot.transform);
        speakerText ??= CreateTextElement("SpeakerText", dialogueRoot.transform);
        bodyText ??= CreateTextElement("BodyText", dialogueRoot.transform);
        advanceHintText ??= CreateTextElement("AdvanceHintText", dialogueRoot.transform);

        ConfigureDialogueRoot();
        ConfigureDayBadge();
        ConfigureDayText();
        ConfigureSpeakerText();
        ConfigureBodyText();
        ConfigureAdvanceHintText();
    }

    // Co gang tim Canvas de dat UI hoi thoai vao do.
    private Canvas ResolveCanvas()
    {
        if (playerUI != null && playerUI.PickUpText != null && playerUI.PickUpText.canvas != null)
        {
            return playerUI.PickUpText.canvas;
        }

        return FindFirstObjectByType<Canvas>();
    }

    // Cai dat vi tri va mau nen cua hop hoi thoai.
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
        rootRect.anchoredPosition = new Vector2(0f, 42f);
        rootRect.sizeDelta = new Vector2(1040f, 228f);

        dialogueBackground.color = new Color(0.04f, 0.08f, 0.12f, 0.86f);
        dialogueBackground.raycastTarget = false;
    }

    private void ConfigureDayBadge()
    {
        if (dayBadgeRoot == null || dayBadgeBackground == null)
        {
            return;
        }

        RectTransform badgeRect = dayBadgeRoot.GetComponent<RectTransform>();
        badgeRect.anchorMin = new Vector2(0f, 1f);
        badgeRect.anchorMax = new Vector2(0f, 1f);
        badgeRect.pivot = new Vector2(0f, 0f);
        badgeRect.anchoredPosition = new Vector2(28f, 18f);
        badgeRect.sizeDelta = new Vector2(178f, 48f);

        dayBadgeBackground.color = new Color(0.89f, 0.57f, 0.18f, 0.96f);
        dayBadgeBackground.raycastTarget = false;
    }

    private void ConfigureDayText()
    {
        if (dayText == null)
        {
            return;
        }

        RectTransform textRect = dayText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.offsetMin = new Vector2(14f, 6f);
        textRect.offsetMax = new Vector2(-14f, -6f);

        dayText.text = string.Empty;
        dayText.color = new Color(0.12f, 0.12f, 0.12f, 1f);
        dayText.alignment = TextAlignmentOptions.Center;
        dayText.textWrappingMode = TextWrappingModes.NoWrap;
        dayText.fontSize = 24f;
        dayText.fontStyle = FontStyles.Bold;
        dayText.raycastTarget = false;
    }

    // Cai dat o hien ten nguoi noi.
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
        textRect.offsetMin = new Vector2(32f, -74f);
        textRect.offsetMax = new Vector2(-32f, -26f);

        speakerText.text = string.Empty;
        speakerText.color = new Color(0.98f, 0.82f, 0.48f, 1f);
        speakerText.alignment = TextAlignmentOptions.TopLeft;
        speakerText.textWrappingMode = TextWrappingModes.NoWrap;
        speakerText.fontSize = 27f;
        speakerText.fontStyle = FontStyles.Bold;
        speakerText.raycastTarget = false;
    }

    // Cai dat o hien noi dung dong thoai.
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
        textRect.offsetMin = new Vector2(32f, 30f);
        textRect.offsetMax = new Vector2(-32f, -84f);

        bodyText.text = string.Empty;
        bodyText.color = new Color(0.96f, 0.97f, 0.98f, 1f);
        bodyText.alignment = TextAlignmentOptions.TopLeft;
        bodyText.textWrappingMode = TextWrappingModes.Normal;
        bodyText.fontSize = 33f;
        bodyText.lineSpacing = -2f;
        bodyText.raycastTarget = false;
    }

    private void ConfigureAdvanceHintText()
    {
        if (advanceHintText == null)
        {
            return;
        }

        RectTransform textRect = advanceHintText.rectTransform;
        textRect.anchorMin = new Vector2(1f, 0f);
        textRect.anchorMax = new Vector2(1f, 0f);
        textRect.pivot = new Vector2(1f, 0f);
        textRect.anchoredPosition = new Vector2(-30f, 18f);
        textRect.sizeDelta = new Vector2(220f, 28f);

        advanceHintText.text = "Click để tiếp tục";
        advanceHintText.color = new Color(0.73f, 0.79f, 0.85f, 0.88f);
        advanceHintText.alignment = TextAlignmentOptions.BottomRight;
        advanceHintText.textWrappingMode = TextWrappingModes.NoWrap;
        advanceHintText.fontSize = 20f;
        advanceHintText.raycastTarget = false;
    }

    // Hien hoac an toan bo khung hoi thoai.
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

    private void RefreshDayBadge(DialogueDay day)
    {
        EnsureDialogueUI();

        if (dayText != null)
        {
            dayText.text = FormatDayLabel(day);
        }
    }

    private static string FormatDayLabel(DialogueDay day)
    {
        int dayNumber = Mathf.Max(1, (int)day);
        return $"DAY {dayNumber}";
    }

    // Ham ho tro de tao nhanh mot o TextMeshPro moi neu scene chua co san.
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
