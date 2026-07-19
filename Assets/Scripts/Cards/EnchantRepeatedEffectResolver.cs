using System;

namespace HaveABreak.Cards
{
    public static class EnchantRepeatedEffectResolver
    {
        public static RepeatedEffectParameters Resolve(
            BattleCardInstance sourceCard,
            BattleCardEnchantRegistry enchants,
            RepeatedEffectParameters original)
        {
            if (sourceCard == null)
            {
                throw new ArgumentNullException(nameof(sourceCard));
            }

            RunCardEnchantState cardEnchants = enchants?.Find(sourceCard.Ids.BattleCardId);
            if (sourceCard.SourceCard.CardType != CardType.Barrier ||
                !sourceCard.SourceCard.HasEnchantCompatibilityTag(
                    EnchantCompatibilityTag.NumericRepeatingEffect) ||
                !HasActiveStarlightEngraving(cardEnchants))
            {
                return original;
            }

            return new RepeatedEffectParameters(
                original.FirstValue + 1,
                original.TargetCount,
                original.ActivationCount,
                original.ConditionThreshold);
        }

        private static bool HasActiveStarlightEngraving(RunCardEnchantState enchants)
        {
            if (enchants == null)
            {
                return false;
            }

            foreach (RunEnchantSlot slot in enchants.Slots)
            {
                if (!slot.IsEmpty && slot.Active && string.Equals(
                        slot.Enchant.DefinitionId,
                        "E06",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
