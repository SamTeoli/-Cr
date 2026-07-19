using System;
using System.Collections.Generic;
using UnityEngine;

namespace HaveABreak.Cards
{
    [Serializable]
    public sealed class EnchantTurnUsageTracker
    {
        [SerializeField] private List<Entry> entries = new();
        [SerializeField] private List<string> processedEventIds = new();

        [Serializable]
        private sealed class Entry
        {
            [SerializeField] private string effectId;
            [SerializeField] private string battleCardId;
            [SerializeField] private int lastPlayerTurn;

            public Entry(string effectId, string battleCardId, int playerTurn)
            {
                this.effectId = effectId;
                this.battleCardId = battleCardId;
                lastPlayerTurn = playerTurn;
            }

            public string EffectId => effectId;
            public string BattleCardId => battleCardId;
            public int LastPlayerTurn => lastPlayerTurn;

            public void MarkTurn(int playerTurn)
            {
                lastPlayerTurn = playerTurn;
            }
        }

        public bool TryUseOncePerPlayerTurn(
            string effectId,
            string battleCardId,
            string sourceEventId,
            int playerTurn)
        {
            if (string.IsNullOrWhiteSpace(effectId) || string.IsNullOrWhiteSpace(battleCardId) ||
                string.IsNullOrWhiteSpace(sourceEventId) || playerTurn < 1 ||
                processedEventIds.Exists(id => string.Equals(
                    id, sourceEventId, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            Entry entry = entries.Find(item => item != null &&
                string.Equals(item.EffectId, effectId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(item.BattleCardId, battleCardId, StringComparison.OrdinalIgnoreCase));
            if (entry != null && entry.LastPlayerTurn == playerTurn)
            {
                processedEventIds.Add(sourceEventId.Trim());
                return false;
            }

            if (entry == null)
            {
                entries.Add(new Entry(effectId.Trim(), battleCardId.Trim(), playerTurn));
            }
            else
            {
                entry.MarkTurn(playerTurn);
            }

            processedEventIds.Add(sourceEventId.Trim());
            return true;
        }
    }
}
