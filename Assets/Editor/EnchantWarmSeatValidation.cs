using System;
using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class EnchantWarmSeatValidation
    {
        [MenuItem("Have a Break/Validate E01 Warm Seat Battle Effect")]
        private static void ValidateFromMenu()
        {
            Validate(true);
        }

        internal static bool Validate(bool showDialog)
        {
            CardData card = FindCard("C01");
            EnchantData enchant = FindEnchant("E01");
            bool valid = card != null && enchant != null;

            if (valid)
            {
                BattleCardInstance battleCard = new(
                    card,
                    new CardInstanceIds(card.CatalogCardId, "OWNED-E01-TEST", "BATTLE-E01-TEST"),
                    1,
                    CardZone.MonsterField);
                int baseHealth = battleCard.Resolved.Health;
                RunCardEnchantState runEnchants = new(card);

                valid &= runEnchants.TryAttach(enchant, 0, false, out EnchantAttachmentFailure failure) &&
                         failure == EnchantAttachmentFailure.None;

                BattleMonsterRegistry registry = new();
                valid &= registry.TryAdd(battleCard, runEnchants, out BattleMonsterState monster) &&
                         monster.BaseMaximumHealth == baseHealth &&
                         monster.MaximumHealth == baseHealth + 2 &&
                         monster.CurrentHealth == baseHealth + 2;

                monster.ApplyDamage(1);
                runEnchants.RefreshCompatibility(CardType.Skill);
                valid &= monster.ApplyEnchantState(runEnchants) &&
                         monster.MaximumHealth == baseHealth &&
                         monster.CurrentHealth == baseHealth;

                runEnchants.RefreshCompatibility(CardType.Monster);
                valid &= monster.ApplyEnchantState(runEnchants) &&
                         monster.MaximumHealth == baseHealth + 2 &&
                         monster.CurrentHealth == baseHealth + 2;

                valid &= monster.ApplyEnchantState(runEnchants) &&
                         monster.MaximumHealth == baseHealth + 2 &&
                         monster.CurrentHealth == baseHealth + 2;

                RunCardEnchantState wrongCardState = new(FindCard("C02"));
                valid &= !monster.ApplyEnchantState(wrongCardState) &&
                         monster.MaximumHealth == baseHealth + 2;
            }
            else
            {
                Debug.LogError("E01 battle effect validation requires C01 and E01.");
            }

            if (!valid)
            {
                Debug.LogError("E01 Warm Seat battle effect validation failed.");
            }

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "E01 Warm Seat Battle Effect Validation",
                    valid
                        ? "E01 Warm Seat battle effect passed."
                        : "E01 Warm Seat battle effect failed. Check the Console.",
                    "OK");
            }

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
