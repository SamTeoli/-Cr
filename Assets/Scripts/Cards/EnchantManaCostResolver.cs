using System;
using UnityEngine;

namespace HaveABreak.Cards
{
    public static class EnchantManaCostResolver
    {
        public static int Resolve(BattleCardInstance card, RunCardEnchantState enchants)
        {
            if (card == null)
            {
                throw new ArgumentNullException(nameof(card));
            }

            int cost = Mathf.Max(0, card.Resolved.ManaCost);
            if (enchants == null || enchants.Card != card.SourceCard)
            {
                return cost;
            }

            foreach (RunEnchantSlot slot in enchants.Slots)
            {
                if (!slot.IsEmpty && slot.Active &&
                    string.Equals(slot.Enchant.DefinitionId, "E04", StringComparison.OrdinalIgnoreCase))
                {
                    cost = Mathf.Max(1, cost - 1);
                }
            }

            return cost;
        }
    }
}
