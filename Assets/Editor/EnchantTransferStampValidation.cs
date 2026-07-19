using System;
using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class EnchantTransferStampValidation
    {
        [MenuItem("Have a Break/Validate E07 Transfer Stamp Graveyard Replacement")]
        private static void ValidateFromMenu()
        {
            Validate(true);
        }

        internal static bool Validate(bool showDialog)
        {
            CardData card = FindCard("C05");
            EnchantData enchant = FindEnchant("E07");
            bool valid = card != null && enchant != null && card.CardType == CardType.Skill;

            if (valid)
            {
                valid &= ValidatePlayedSkill(card, enchant);
                valid &= ValidateNonResolutionMove(card, enchant);
            }
            else
            {
                Debug.LogError("E07 graveyard replacement validation requires C05 and E07.");
            }

            if (!valid)
            {
                Debug.LogError("E07 Transfer Stamp graveyard replacement validation failed.");
            }

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "E07 Transfer Stamp Graveyard Replacement Validation",
                    valid
                        ? "E07 Transfer Stamp graveyard replacement passed."
                        : "E07 Transfer Stamp graveyard replacement failed. Check the Console.",
                    "OK");
            }

            return valid;
        }

        private static bool ValidatePlayedSkill(CardData card, EnchantData enchant)
        {
            BattleCardInstance battleCard = CreateBattleCard(card, "PLAY");
            BattleDeckState deck = new(new[] { battleCard }, 2107);
            bool valid = deck.DrawStartingHand() == 1;
            RunCardEnchantState runEnchants = CreateEnchantState(card, enchant, ref valid);
            BattleCardEnchantRegistry registry = new();
            valid &= registry.TryRegister(battleCard, runEnchants);

            BattleCardPlayState play = new(deck, BattleManaState.DefaultMaximumMana, registry);
            valid &= play.TryPreviewPlay(battleCard.Ids.BattleCardId, out CardPlayPreview firstPreview, out _) &&
                     play.TryConfirmPlay(firstPreview, out _) &&
                     battleCard.Zone == CardZone.DrawPile &&
                     deck.DrawPileOrder.Count == 1 &&
                     deck.DrawPileOrder[deck.DrawPileOrder.Count - 1] == battleCard.Ids.BattleCardId &&
                     !registry.HasAvailableTransferStamp(battleCard.Ids.BattleCardId);

            valid &= deck.TryDraw(out BattleCardInstance redrawn, out _) && redrawn == battleCard;
            play.Mana.StartPlayerTurn();
            valid &= play.TryPreviewPlay(battleCard.Ids.BattleCardId, out CardPlayPreview secondPreview, out _) &&
                     play.TryConfirmPlay(secondPreview, out _) &&
                     battleCard.Zone == CardZone.Graveyard &&
                     deck.DrawPileOrder.Count == 0;
            return valid;
        }

        private static bool ValidateNonResolutionMove(CardData card, EnchantData enchant)
        {
            BattleCardInstance battleCard = CreateBattleCard(card, "FORCED");
            BattleDeckState deck = new(new[] { battleCard }, 2117);
            bool valid = deck.DrawStartingHand() == 1;
            RunCardEnchantState runEnchants = CreateEnchantState(card, enchant, ref valid);
            BattleCardEnchantRegistry registry = new();
            valid &= registry.TryRegister(battleCard, runEnchants);

            BattleEventLog log = new();
            BattleEventRecord root = log.Record(
                BattleEventType.CardPlayed,
                "E07Validation",
                battleCard.Ids.BattleCardId,
                battleCard.Ids.BattleCardId,
                battleCard.Ids.BattleCardId);
            BattleEffectCommand forcedMove = new(
                "E07-FORCED-MOVE",
                battleCard.Ids.BattleCardId,
                root.EventId,
                EffectProcessingStage.MainEffect,
                true,
                BattleEventType.CardPlayed,
                operation: EffectOperation.Move,
                targetBattleCardId: battleCard.Ids.BattleCardId,
                destinationZone: CardZone.Graveyard,
                hasDestinationZone: true,
                normalResolutionGraveyardMove: false);
            BattleEffectQueue queue = new();
            valid &= queue.TryRegister(forcedMove, root, out _);
            BattleEffectExecutor executor = new(deck, log, queue, enchants: registry);
            valid &= executor.TryExecuteNext(out _, out BattleEventRecord moved, out _) &&
                     moved.ToZone == CardZone.Graveyard &&
                     battleCard.Zone == CardZone.Graveyard &&
                     registry.HasAvailableTransferStamp(battleCard.Ids.BattleCardId);
            return valid;
        }

        private static BattleCardInstance CreateBattleCard(CardData card, string suffix)
        {
            return new BattleCardInstance(
                card,
                new CardInstanceIds(
                    card.CatalogCardId,
                    $"OWNED-E07-{suffix}",
                    $"BATTLE-E07-{suffix}"),
                1,
                CardZone.DrawPile);
        }

        private static RunCardEnchantState CreateEnchantState(
            CardData card,
            EnchantData enchant,
            ref bool valid)
        {
            RunCardEnchantState state = new(card);
            valid &= state.TryAttach(enchant, 0, false, out EnchantAttachmentFailure failure) &&
                     failure == EnchantAttachmentFailure.None;
            return state;
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
