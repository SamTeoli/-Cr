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
            [SerializeField] private bool transferStampUsed;

            public Entry(string battleCardId, RunCardEnchantState enchantState)
            {
                this.battleCardId = battleCardId;
                this.enchantState = enchantState;
            }

            public string BattleCardId => battleCardId;
            public RunCardEnchantState EnchantState => enchantState;
            public bool TransferStampUsed => transferStampUsed;

            public void MarkTransferStampUsed()
            {
                transferStampUsed = true;
            }
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

        public bool HasAvailableTransferStamp(string battleCardId)
        {
            Entry entry = FindEntry(battleCardId);
            if (entry == null || entry.TransferStampUsed)
            {
                return false;
            }

            foreach (RunEnchantSlot slot in entry.EnchantState.Slots)
            {
                if (!slot.IsEmpty && slot.Active && string.Equals(
                        slot.Enchant.DefinitionId,
                        "E07",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public bool MarkTransferStampUsed(string battleCardId)
        {
            Entry entry = FindEntry(battleCardId);
            if (entry == null || entry.TransferStampUsed)
            {
                return false;
            }

            entry.MarkTransferStampUsed();
            return true;
        }

        private Entry FindEntry(string battleCardId)
        {
            if (string.IsNullOrWhiteSpace(battleCardId))
            {
                return null;
            }

            return entries.Find(item => item != null && string.Equals(
                item.BattleCardId, battleCardId, StringComparison.OrdinalIgnoreCase));
        }
    }
}
