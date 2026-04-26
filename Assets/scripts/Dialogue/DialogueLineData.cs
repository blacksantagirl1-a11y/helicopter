using System;
using UnityEngine;

[Serializable]
// DialogueLineData la 1 dong thoai don le:
// ai noi + noi cau gi.
public sealed class DialogueLineData
{
    // Ten nguoi noi. Co the de trong neu muon hien nhu loi dan / narration.
    [SerializeField] private string speakerName;
    [TextArea(2, 5)]
    // Noi dung cau noi se hien len man hinh.
    [SerializeField] private string text;
    [SerializeField] private DialogueQuestAction questAction;
    [SerializeField] private DailyQuestId questId;

    public string SpeakerName => speakerName;
    public string Text => text;
    public DialogueQuestAction QuestAction => questAction;
    public DailyQuestId QuestId => questId;
}
