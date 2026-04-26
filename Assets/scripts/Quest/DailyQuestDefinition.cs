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
    [SerializeField]
    [Min(30f)]
    private float dayDurationSeconds = 300f;
    [SerializeField] private DialogueEventId completionDialogueEvent = DialogueEventId.DoneRequest;

    public DialogueDay Day => day;
    public DailyQuestId QuestId => questId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? questId.ToString() : displayName;
    public QuestObjectiveType ObjectiveType => objectiveType;
    public string InteractionKey => interactionKey;
    public InventoryItemDefinition TargetItem => targetItem;
    public int RequiredCount => Mathf.Max(1, requiredCount);
    public float DayDurationSeconds => Mathf.Max(30f, dayDurationSeconds);
    public DialogueEventId CompletionDialogueEvent => completionDialogueEvent;
}
