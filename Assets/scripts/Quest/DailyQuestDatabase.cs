using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DailyQuestDatabase", menuName = "Quest/Daily Quest Database")]
public class DailyQuestDatabase : ScriptableObject
{
    [SerializeField] private List<DailyQuestDefinition> quests = new List<DailyQuestDefinition>();

    public IReadOnlyList<DailyQuestDefinition> Quests => quests;

    public bool TryGetQuest(DialogueDay day, DailyQuestId questId, out DailyQuestDefinition quest)
    {
        if (quests != null)
        {
            for (int index = 0; index < quests.Count; index++)
            {
                DailyQuestDefinition candidate = quests[index];
                if (candidate == null)
                {
                    continue;
                }

                if (candidate.Day == day && candidate.QuestId == questId)
                {
                    quest = candidate;
                    return true;
                }
            }
        }

        quest = null;
        return false;
    }

    public bool HasQuestForDay(DialogueDay day)
    {
        if (quests == null)
        {
            return false;
        }

        for (int index = 0; index < quests.Count; index++)
        {
            DailyQuestDefinition quest = quests[index];
            if (quest != null && quest.Day == day && quest.QuestId != DailyQuestId.None)
            {
                return true;
            }
        }

        return false;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (quests == null)
        {
            return;
        }

        HashSet<string> seenKeys = new HashSet<string>();
        for (int index = 0; index < quests.Count; index++)
        {
            DailyQuestDefinition quest = quests[index];
            if (quest == null)
            {
                continue;
            }

            string compositeKey = $"{quest.Day}:{quest.QuestId}";
            if (!seenKeys.Add(compositeKey))
            {
                Debug.LogWarning($"Duplicate daily quest definition found for {compositeKey}.", this);
            }
        }
    }
#endif
}
