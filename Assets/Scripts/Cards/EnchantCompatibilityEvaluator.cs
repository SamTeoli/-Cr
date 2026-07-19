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
                "E01" => card is MonsterCardData healthMonster && healthMonster.Health > 0,
                "E02" => card is MonsterCardData attackMonster && attackMonster.Attack > 0,
                "E03" => card.HasEnchantCompatibilityTag(
                    EnchantCompatibilityTag.MainEffectCompletion),
                "E04" => card.CardType == CardType.Skill && card.ManaCost >= 2,
                "E05" => card.HasEnchantCompatibilityTag(
                    EnchantCompatibilityTag.EnemyAffectingEffect),
                "E06" => card.HasEnchantCompatibilityTag(
                    EnchantCompatibilityTag.NumericRepeatingEffect),
                "E07" => card.HasEnchantCompatibilityTag(
                    EnchantCompatibilityTag.NormalGraveyardAfterResolution),
                "E08" => card.HasEnchantCompatibilityTag(
                    EnchantCompatibilityTag.FixedSingleEnemyTarget),
                _ => string.IsNullOrWhiteSpace(enchant.AdditionalCompatibilityRule)
            };
        }
    }
}
