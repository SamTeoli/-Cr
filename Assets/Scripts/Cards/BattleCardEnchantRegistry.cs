using System;
using System.Collections.Generic;
using UnityEngine;

namespace HaveABreak.Cards
{
    [Serializable]
    public sealed class BattleCardEnchantRegistry
    {
        [SerializeField] private List<Entry> entries = new();

        [Serializable]
        private sealed class Entry
        {
            [SerializeField] private string battleCardId;
            [SerializeField] private RunCardEnchantState enchantState;

            public Entry(string battleCardId, RunCardEnchantState enchantState)
            {
                this.battleCardId = battleCardId;
                this.enchantState = enchantState;
            }

            public string BattleCardId => battleCardId;
            public RunCardEnchantState EnchantState => enchantState;
        }

        public bool TryRegister(BattleCardInstance card, RunCardEnchantState enchantState)
        {
            if (card == null || enchantState == null || enchantState.Card != card.SourceCard ||
                Find(card.Ids.BattleCardId) != null)
            {
                return false;
            }

            entries.Add(new Entry(card.Ids.BattleCardId, enchantState));
            return true;
        }

        public RunCardEnchantState Find(string battleCardId)
        {
            if (string.IsNullOrWhiteSpace(battleCardId))
            {
                return null;
            }

            Entry entry = entries.Find(item => item != null && string.Equals(
                item.BattleCardId, battleCardId, StringComparison.OrdinalIgnoreCase));
            return entry?.EnchantState;
        }
    }
}
