using System;
using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class EnchantBackupPowerValidation
    {
        [MenuItem("Have a Break/Validate E04 Backup Power Battle Cost")]
        private static void ValidateFromMenu()
        {
            Validate(true);
        }

        internal static bool Validate(bool showDialog)
        {
            CardData card = FindCard("C06");
            EnchantData enchant = FindEnchant("E04");
            bool valid = card != null && enchant != null && card.CardType == CardType.Skill;

            if (valid)
            {
                BattleCardInstance battleCard = new(
                    card,
                    new CardInstanceIds(card.CatalogCardId, "OWNED-E04-TEST", "BATTLE-E04-TEST"),
                    1,
                    CardZone.DrawPile);
                BattleDeckState deck = new(new[] { battleCard }, 1904);
                valid &= deck.DrawStartingHand() == 1 && battleCard.Zone == CardZone.Hand;

                RunCardEnchantState runEnchants = new(card);
                valid &= runEnchants.TryAttach(enchant, 0, false, out EnchantAttachmentFailure attachFailure) &&
                         attachFailure == EnchantAttachmentFailure.None;

                BattleCardEnchantRegistry registry = new();
                valid &= registry.TryRegister(battleCard, runEnchants) &&
                         !registry.TryRegister(battleCard, runEnchants) &&
                         registry.Find(battleCard.Ids.BattleCardId) == runEnchants;

                int baseCost = battleCard.Resolved.ManaCost;
                int expectedCost = Mathf.Max(1, baseCost - 1);
                BattleCardPlayState play = new(deck, BattleManaState.DefaultMaximumMana, registry);
                valid &= play.TryPreviewPlay(
                             battleCard.Ids.BattleCardId,
                             out CardPlayPreview activePreview,
                             out CardPlayFailure previewFailure) &&
                         previewFailure == CardPlayFailure.None &&
                         activePreview.ManaCost == expectedCost;

                runEnchants.RefreshCompatibility(CardType.Monster);
                valid &= EnchantManaCostResolver.Resolve(battleCard, runEnchants) == baseCost &&
                         !play.TryConfirmPlay(activePreview, out CardPlayFailure staleFailure) &&
                         staleFailure == CardPlayFailure.InvalidPreview &&
                         play.Mana.CurrentMana == BattleManaState.DefaultMaximumMana &&
                         battleCard.Zone == CardZone.Hand;

                runEnchants.RefreshCompatibility(CardType.Skill);
                valid &= play.TryConfirmPlay(activePreview, out CardPlayFailure confirmFailure) &&
                         confirmFailure == CardPlayFailure.None &&
                         play.Mana.CurrentMana == BattleManaState.DefaultMaximumMana - expectedCost &&
                         battleCard.Zone == CardZone.Graveyard;
            }
            else
            {
                Debug.LogError("E04 battle cost validation requires C06 and E04.");
            }

            if (!valid)
            {
                Debug.LogError("E04 Backup Power battle cost validation failed.");
            }

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "E04 Backup Power Battle Cost Validation",
                    valid
                        ? "E04 Backup Power battle cost passed."
                        : "E04 Backup Power battle cost failed. Check the Console.",
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
