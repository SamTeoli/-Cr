using System;
using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class EnchantStarlightEngravingValidation
    {
        [MenuItem("Have a Break/Validate E06 Starlight Engraving Repeated Value")]
        private static void ValidateFromMenu()
        {
            Validate(true);
        }

        internal static bool Validate(bool showDialog)
        {
            EnchantData enchant = FindEnchant("E06");
            CardData c11 = FindCard("C11");
            CardData c12 = FindCard("C12");
            bool valid = enchant != null && c11 != null && c12 != null;

            if (valid)
            {
                valid &= ValidateCard(c11, enchant, 2011);
                valid &= ValidateCard(c12, enchant, 2012);
            }
            else
            {
                Debug.LogError("E06 repeated value validation requires C11, C12 and E06.");
            }

            if (!valid)
            {
                Debug.LogError("E06 Starlight Engraving repeated value validation failed.");
            }

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "E06 Starlight Engraving Repeated Value Validation",
                    valid
                        ? "E06 Starlight Engraving repeated value passed."
                        : "E06 Starlight Engraving repeated value failed. Check the Console.",
                    "OK");
            }

            return valid;
        }

        private static bool ValidateCard(CardData card, EnchantData enchant, int suffix)
        {
            BattleCardInstance battleCard = new(
                card,
                new CardInstanceIds(
                    card.CatalogCardId,
                    $"OWNED-E06-{suffix}",
                    $"BATTLE-E06-{suffix}"),
                1,
                CardZone.SkillField);
            RunCardEnchantState runEnchants = new(card);
            bool valid = runEnchants.TryAttach(
                enchant, 0, false, out EnchantAttachmentFailure attachFailure) &&
                         attachFailure == EnchantAttachmentFailure.None;

            BattleCardEnchantRegistry registry = new();
            valid &= registry.TryRegister(battleCard, runEnchants);

            RepeatedEffectParameters original = new(7, 3, 2, 4);
            RepeatedEffectParameters active = EnchantRepeatedEffectResolver.Resolve(
                battleCard, registry, original);
            valid &= active.FirstValue == 8 &&
                     active.TargetCount == original.TargetCount &&
                     active.ActivationCount == original.ActivationCount &&
                     active.ConditionThreshold == original.ConditionThreshold;

            runEnchants.RefreshCompatibility(CardType.Skill);
            RepeatedEffectParameters inactive = EnchantRepeatedEffectResolver.Resolve(
                battleCard, registry, original);
            valid &= inactive.FirstValue == original.FirstValue &&
                     inactive.TargetCount == original.TargetCount &&
                     inactive.ActivationCount == original.ActivationCount &&
                     inactive.ConditionThreshold == original.ConditionThreshold;

            runEnchants.RefreshCompatibility(CardType.Barrier);
            RepeatedEffectParameters restored = EnchantRepeatedEffectResolver.Resolve(
                battleCard, registry, original);
            valid &= restored.FirstValue == original.FirstValue + 1;

            RepeatedEffectParameters unregistered = EnchantRepeatedEffectResolver.Resolve(
                battleCard, null, original);
            valid &= unregistered.FirstValue == original.FirstValue;
            return valid;
        }

        private static CardData FindCard(string id)
        {
            return AssetDatabase.FindAssets("t:CardData")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(path => AssetDatabase.LoadAssetAtPath<CardData>(path))
                .FirstOrDefault(card => card != null && string.Equals(
                    card.CatalogCardId, id, StringComparison.OrdinalIgnoreCase));
        }

        private static EnchantData FindEnchant(string id)
        {
            return AssetDatabase.FindAssets("t:EnchantData")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(path => AssetDatabase.LoadAssetAtPath<EnchantData>(path))
                .FirstOrDefault(enchant => enchant != null && string.Equals(
                    enchant.DefinitionId, id, StringComparison.OrdinalIgnoreCase));
        }
    }
}
