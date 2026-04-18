using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DialogueDatabase", menuName = "Dialogue/Dialogue Database")]
public class DialogueDatabase : ScriptableObject
{
    [SerializeField] private List<DialogueEntry> entries = new List<DialogueEntry>();

    public IReadOnlyList<DialogueEntry> Entries => entries;

    public bool TryGetEntry(DialogueDay day, DialogueEventId eventId, out DialogueEntry entry)
    {
        if (entries != null)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                DialogueEntry candidate = entries[i];
                if (candidate == null)
                {
                    continue;
                }

                if (candidate.Day == day && candidate.EventId == eventId)
                {
                    entry = candidate;
                    return true;
                }
            }
        }

        entry = null;
        return false;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (entries == null)
        {
            return;
        }

        HashSet<string> seenKeys = new HashSet<string>();
        for (int i = 0; i < entries.Count; i++)
        {
            DialogueEntry entry = entries[i];
            if (entry == null)
            {
                Debug.LogWarning($"DialogueDatabase '{name}' has a null entry at index {i}.", this);
                continue;
            }

            string compositeKey = $"{entry.Day}:{entry.EventId}";
            if (!seenKeys.Add(compositeKey))
            {
                Debug.LogWarning(
                    $"DialogueDatabase '{name}' has duplicate dialogue data for {entry.Day} / {entry.EventId}.",
                    this);
            }

            if (entry.EventId == DialogueEventId.None)
            {
                Debug.LogWarning($"DialogueDatabase '{name}' has an entry using DialogueEventId.None.", this);
            }

            if (entry.LineCount < 1 || entry.LineCount > 5)
            {
                Debug.LogWarning(
                    $"DialogueDatabase '{name}' entry {entry.Day} / {entry.EventId} must contain between 1 and 5 lines.",
                    this);
            }

            for (int lineIndex = 0; lineIndex < entry.LineCount; lineIndex++)
            {
                if (!entry.TryGetLine(lineIndex, out DialogueLineData line) || line == null)
                {
                    Debug.LogWarning(
                        $"DialogueDatabase '{name}' entry {entry.Day} / {entry.EventId} has a null line at index {lineIndex}.",
                        this);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(line.Text))
                {
                    Debug.LogWarning(
                        $"DialogueDatabase '{name}' entry {entry.Day} / {entry.EventId} has an empty line at index {lineIndex}.",
                        this);
                }
            }
        }
    }
#endif
}
