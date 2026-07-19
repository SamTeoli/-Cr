using System;
using System.Collections.Generic;
using UnityEngine;

namespace HaveABreak.Cards
{
    [Serializable]
    public sealed class BattleCardTurnTriggerState
    {
        [Serializable]
        private sealed class Entry
        {
            public string Key;
            public int Count;
            public List<string> ProcessedKeys = new();
        }

        [SerializeField] private List<Entry> entries = new();

        public bool TryUse(
            string effectId, string sourceBattleCardId, int turnNumber,
            string eventOrCommandKey, int maximumUses)
        {
            if (string.IsNullOrWhiteSpace(effectId) ||
                string.IsNullOrWhiteSpace(sourceBattleCardId) ||
                string.IsNullOrWhiteSpace(eventOrCommandKey) ||
                turnNumber < 1 || maximumUses < 1)
            {
                return false;
            }

            string key = $"{effectId}:{sourceBattleCardId}:TURN-{turnNumber}";
            Entry entry = entries.Find(item => item != null &&
                string.Equals(item.Key, key, StringComparison.OrdinalIgnoreCase));
            if (entry == null)
            {
                entry = new Entry { Key = key };
                entries.Add(entry);
            }

            if (entry.Count >= maximumUses || entry.ProcessedKeys.Exists(item =>
                    string.Equals(item, eventOrCommandKey, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            entry.ProcessedKeys.Add(eventOrCommandKey);
            entry.Count++;
            return true;
        }
    }
}
