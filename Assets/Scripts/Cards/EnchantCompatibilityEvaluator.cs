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

            if (!EnchantEffectRegistrationCatalog.TryFind(
                    enchant.DefinitionId, out EnchantEffectRegistration registration))
            {
                return string.IsNullOrWhiteSpace(enchant.AdditionalCompatibilityRule);
            }

            return registration.Compatibility switch
            {
                EnchantCompatibilityRequirement.PositiveHealthMonster =>
                    card is MonsterCardData healthMonster && healthMonster.Health > 0,
                EnchantCompatibilityRequirement.PositiveAttackMonster =>
                    card is MonsterCardData attackMonster && attackMonster.Attack > 0,
                EnchantCompatibilityRequirement.MainEffectCompletion => card.HasEnchantCompatibilityTag(
                    EnchantCompatibilityTag.MainEffectCompletion),
                EnchantCompatibilityRequirement.SkillCostAtLeastTwo =>
                    card.CardType == CardType.Skill && card.ManaCost >= 2,
                EnchantCompatibilityRequirement.EnemyAffectingEffect => card.HasEnchantCompatibilityTag(
                    EnchantCompatibilityTag.EnemyAffectingEffect),
                EnchantCompatibilityRequirement.NumericRepeatingEffect => card.HasEnchantCompatibilityTag(
                    EnchantCompatibilityTag.NumericRepeatingEffect),
                EnchantCompatibilityRequirement.NormalGraveyardAfterResolution => card.HasEnchantCompatibilityTag(
                    EnchantCompatibilityTag.NormalGraveyardAfterResolution),
                EnchantCompatibilityRequirement.FixedSingleEnemyTarget => card.HasEnchantCompatibilityTag(
                    EnchantCompatibilityTag.FixedSingleEnemyTarget),
                _ => true
            };
        }
    }
}
