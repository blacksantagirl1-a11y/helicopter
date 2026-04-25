using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
// DialogueEntry = 1 doan hoi thoai hoan chinh cho 1 su kien trong 1 ngay cu the.
public sealed class DialogueEntry
{
    // Doan hoi thoai nay thuoc ngay nao trong tien trinh cau chuyen.
    [SerializeField] private DialogueDay day = DialogueDay.Day1;
    // Su kien nao se kich hoat doan hoi thoai nay.
    [SerializeField] private DialogueEventId eventId = DialogueEventId.None;
    // Trong luc hoi thoai nay chay, nguoi choi co duoc phep di chuyen hay khong.
    [SerializeField] private bool playerCanMove;
    [SerializeField]
    [Min(0f)]
    // Time.timeScale se tam thoi doi theo gia tri nay trong luc hoi thoai.
    // Vi du: 1 = binh thuong, 0 = dung game, 0.5 = cham hon.
    private float timeScale = 1f;
    // Cac dong thoai ben trong doan hoi thoai.
    [SerializeField] private List<DialogueLineData> lines = new List<DialogueLineData>();

    public DialogueDay Day => day;
    public DialogueEventId EventId => eventId;
    public bool PlayerCanMove => playerCanMove;
    public float TimeScale => Mathf.Max(0f, timeScale);
    public IReadOnlyList<DialogueLineData> Lines => lines;
    public int LineCount => lines != null ? lines.Count : 0;

    // Lay ra 1 dong thoai theo chi so thu tu.
    // index = 0 la dong dau tien, 1 la dong thu hai...
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
