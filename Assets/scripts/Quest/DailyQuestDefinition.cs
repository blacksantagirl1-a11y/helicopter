using System;
using UnityEngine;

[Serializable]
public sealed class DailyQuestDefinition
{
    [SerializeField] private DialogueDay day = DialogueDay.Day1;
    [SerializeField] private DailyQuestId questId = DailyQuestId.None;
    [SerializeField] private string displayName = "Quest";
    [SerializeField] private QuestObjectiveType objectiveType = QuestObjectiveType.None;
    [SerializeField] private string interactionKey = string.Empty;
    [SerializeField] private InventoryItemDefinition targetItem;
    [SerializeField]
    [Min(1)]
    private int requiredCount = 1;
    [SerializeField] private DialogueEventId completionDialogueEvent = DialogueEventId.DoneRequest;
    [SerializeField] private bool requiresTurnInAfterCompletionDialogue;
    [SerializeField] private DialogueEventId turnInCompletionDialogueEvent = DialogueEventId.DoneRequest;

    public DialogueDay Day => day;
    public DailyQuestId QuestId => questId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? questId.ToString() : displayName;
    public QuestObjectiveType ObjectiveType => objectiveType;
    public string InteractionKey => interactionKey;
    public InventoryItemDefinition TargetItem => targetItem;
    public int RequiredCount => Mathf.Max(1, requiredCount);
    public DialogueEventId CompletionDialogueEvent => completionDialogueEvent;
    public bool RequiresTurnInAfterCompletionDialogue => requiresTurnInAfterCompletionDialogue;
    public DialogueEventId TurnInCompletionDialogueEvent => turnInCompletionDialogueEvent;
}
