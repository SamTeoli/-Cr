using System;

namespace HaveABreak.Cards
{
    public static class EnchantCompatibilityEvaluator
    {
        public static bool IsCompatible(EnchantData enchant, CardData card)
        {
            if (enchant == null || card == null || !enchant.IsCompatible(card.CardType))
            {
                return false;
            }

            return enchant.DefinitionId?.ToUpperInvariant() switch
            {
                TestContentIds.E01 => card is MonsterCardData healthMonster && healthMonster.Health > 0,
                TestContentIds.E02 => card is MonsterCardData attackMonster && attackMonster.Attack > 0,
                TestContentIds.E03 => card.HasEnchantCompatibilityTag(
                    EnchantCompatibilityTag.MainEffectCompletion),
                TestContentIds.E04 => card.CardType == CardType.Skill && card.ManaCost >= 2,
                TestContentIds.E05 => card.HasEnchantCompatibilityTag(
                    EnchantCompatibilityTag.EnemyAffectingEffect),
                TestContentIds.E06 => card.HasEnchantCompatibilityTag(
                    EnchantCompatibilityTag.NumericRepeatingEffect),
                TestContentIds.E07 => card.HasEnchantCompatibilityTag(
                    EnchantCompatibilityTag.NormalGraveyardAfterResolution),
                TestContentIds.E08 => card.HasEnchantCompatibilityTag(
                    EnchantCompatibilityTag.FixedSingleEnemyTarget),
                _ => string.IsNullOrWhiteSpace(enchant.AdditionalCompatibilityRule)
            };
        }
    }
}
