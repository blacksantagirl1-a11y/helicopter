using UnityEngine;

[DisallowMultipleComponent]
public sealed class HintDay3Interactable : Interactable
{
    [SerializeField] private string promptText = "Kiem tra";
    [SerializeField] private DialogueDay requiredDay = DialogueDay.Day3;
    [SerializeField] private DialogueEventId dialogueEventId = DialogueEventId.InvestigationProgress;
    [SerializeField] private float glitchDistance = 10f;
    [SerializeField, Range(0f, 1f)] private float maxGlitchAmount = 0.5f;

    private bool isInteractionPending;
    private bool hasCompleted;
    private Transform playerTransform;

    public override bool CanInteract =>
        !hasCompleted &&
        !isInteractionPending &&
        DialogueController.GetCurrentDay() == requiredDay &&
        !DialogueController.IsDialogueActive;

    public override string PromptText => promptText;

    private void Awake()
    {
        EnsureCollider();
        RefreshPersistentState();
        ResolvePlayerTransform();
        SetGlitchAmount(0f);
    }

    private void OnEnable()
    {
        DialogueController.DialogueFinished += HandleDialogueFinished;
        DialogueSaveService.CurrentDayChanged += HandleCurrentDayChanged;
        RefreshPersistentState();
        SetGlitchAmount(0f);
    }

    private void OnDisable()
    {
        DialogueController.DialogueFinished -= HandleDialogueFinished;
        DialogueSaveService.CurrentDayChanged -= HandleCurrentDayChanged;
        isInteractionPending = false;
        SetGlitchAmount(0f);
    }

    private void Update()
    {
        if (hasCompleted || DialogueController.IsDialogueActive)
        {
            SetGlitchAmount(0f);
            return;
        }

        Transform target = ResolvePlayerTransform();
        if (target == null)
        {
            SetGlitchAmount(0f);
            return;
        }

        float normalizedDistance = 1f - Mathf.Clamp01(Vector3.Distance(target.position, transform.position) / Mathf.Max(0.01f, glitchDistance));
        float glitchAmount = Mathf.SmoothStep(0f, 1f, normalizedDistance);
        SetGlitchAmount(glitchAmount);
    }

    public void Configure(DialogueDay day, DialogueEventId eventId)
    {
        requiredDay = day;
        dialogueEventId = eventId;
        RefreshPersistentState();
        EnsureCollider();
        SetGlitchAmount(0f);
    }

    protected override void Interact()
    {
        if (!CanInteract)
        {
            return;
        }

        if (dialogueEventId == DialogueEventId.None)
        {
            CompleteInteraction();
            return;
        }

        isInteractionPending = DialogueController.RequestDialogue(dialogueEventId);
    }

    protected override void PresentInteraction(PlayerUI playerUI)
    {
        if (playerUI != null)
        {
            playerUI.HideInteractionContent();
        }
    }

    private void HandleDialogueFinished(DialogueDay day, DialogueEventId eventId)
    {
        if (!isInteractionPending || day != requiredDay || eventId != dialogueEventId)
        {
            return;
        }

        CompleteInteraction();
    }

    private void HandleCurrentDayChanged(DialogueDay day)
    {
        RefreshPersistentState();
        if (day != requiredDay)
        {
            SetGlitchAmount(0f);
        }
    }

    private void CompleteInteraction()
    {
        isInteractionPending = false;
        hasCompleted = true;
        Day3HintSequenceController.MarkHintCompleted();
        SetGlitchAmount(0f);
        gameObject.SetActive(false);
    }

    private void RefreshPersistentState()
    {
        hasCompleted = Day3HintSequenceController.IsHintCompleted();
    }

    private Transform ResolvePlayerTransform()
    {
        if (playerTransform != null)
        {
            return playerTransform;
        }

        PlayerMovement playerMovement = FindFirstObjectByType<PlayerMovement>();
        if (playerMovement != null)
        {
            playerTransform = playerMovement.transform;
            return playerTransform;
        }

        PickUpScript pickUpScript = FindFirstObjectByType<PickUpScript>();
        if (pickUpScript != null)
        {
            playerTransform = pickUpScript.transform;
        }

        return playerTransform;
    }

    private void SetGlitchAmount(float amount)
    {
        HintDay3KinoGlitchState.SetAmount(Mathf.Clamp01(amount) * maxGlitchAmount);
    }

    private void EnsureCollider()
    {
        BoxCollider boxCollider = GetComponent<BoxCollider>();
        if (boxCollider == null)
        {
            boxCollider = gameObject.AddComponent<BoxCollider>();
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
        {
            boxCollider.center = Vector3.zero;
            boxCollider.size = Vector3.one;
            return;
        }

        Bounds bounds = renderers[0].bounds;
        for (int index = 1; index < renderers.Length; index++)
        {
            bounds.Encapsulate(renderers[index].bounds);
        }

        Vector3 lossyScale = transform.lossyScale;
        boxCollider.center = transform.InverseTransformPoint(bounds.center);
        boxCollider.size = new Vector3(
            SafeInverseScale(bounds.size.x, lossyScale.x),
            SafeInverseScale(bounds.size.y, lossyScale.y),
            SafeInverseScale(bounds.size.z, lossyScale.z));
        boxCollider.isTrigger = false;
    }

    private static float SafeInverseScale(float worldSize, float scale)
    {
        float absoluteScale = Mathf.Abs(scale);
        return absoluteScale > 0.0001f ? worldSize / absoluteScale : worldSize;
    }
}
