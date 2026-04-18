using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class DialogueEntry
{
    [SerializeField] private DialogueDay day = DialogueDay.Day1;
    [SerializeField] private DialogueEventId eventId = DialogueEventId.None;
    [SerializeField] private bool playerCanMove;
    [SerializeField]
    [Min(0f)]
    private float timeScale = 1f;
    [SerializeField] private List<DialogueLineData> lines = new List<DialogueLineData>();

    public DialogueDay Day => day;
    public DialogueEventId EventId => eventId;
    public bool PlayerCanMove => playerCanMove;
    public float TimeScale => Mathf.Max(0f, timeScale);
    public IReadOnlyList<DialogueLineData> Lines => lines;
    public int LineCount => lines != null ? lines.Count : 0;

    public bool TryGetLine(int index, out DialogueLineData line)
    {
        if (lines != null && index >= 0 && index < lines.Count)
        {
            line = lines[index];
            return true;
        }

        line = null;
        return false;
    }
}
