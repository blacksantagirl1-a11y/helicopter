using System;
using UnityEngine;

[Serializable]
public sealed class DialogueLineData
{
    [SerializeField] private string speakerName;
    [TextArea(2, 5)]
    [SerializeField] private string text;

    public string SpeakerName => speakerName;
    public string Text => text;
}
