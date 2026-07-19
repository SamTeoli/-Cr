using System;
using System.Collections.Generic;
using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class EnchantRoundTripTicketValidation
    {
        [MenuItem("Have a Break/Validate E03 Round Trip Ticket Main Effect Draw")]
        private static void ValidateFromMenu()
        {
            Validate(true);
        }

        internal static bool Validate(bool showDialog)
        {
            CardData sourceCard = FindCard("C05");
            CardData fillerCard = FindCard("C01");
            EnchantData enchant = FindEnchant("E03");
            bool valid = sourceCard != null && fillerCard != null && enchant != null;

            if (valid)
            {
                valid &= ValidateSuccessfulDraws(sourceCard, fillerCard, enchant);
                valid &= ValidateFullHand(sourceCard, fillerCard, enchant);
            }
            else
            {
                Debug.LogError("E03 draw validation requires C05, C01 and E03.");
            }

            if (!valid)
            {
                Debug.LogError("E03 Round Trip Ticket main effect draw validation failed.");
            }

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "E03 Round Trip Ticket Main Effect Draw Validation",
                    valid
                        ? "E03 Round Trip Ticket main effect draw passed."
                        : "E03 Round Trip Ticket main effect draw failed. Check the Console.",
                    "OK");
            }

            return valid;
        }

        private static bool ValidateSuccessfulDraws(
            CardData sourceCard,
            CardData fillerCard,
            EnchantData enchant)
        {
            BattleCardInstance source = CreateCard(sourceCard, "SOURCE", CardZone.Graveyard);
            BattleCardEnchantRegistry registry = CreateRegistry(source, enchant, out bool valid);
            BattleDeckState deck = new(new[]
            {
                CreateCard(fillerCard, "DRAW-1", CardZone.DrawPile),
                CreateCard(fillerCard, "DRAW-2", CardZone.DrawPile)
            }, 2403);
            BattleManaState zeroMana = new(0);
            BattleEventLog log = new();
            EnchantTurnUsageTracker usage = new();

            BattleEventRecord firstRoot = RecordRoot(log, source, "ROOT-1");
            valid &= BattleMainEffectEventService.TryRecordCompleted(
                log, firstRoot, source.Ids.BattleCardId, out BattleEventRecord firstCompleted);
            valid &= !BattleMainEffectEventService.TryRecordCompleted(
                log, firstRoot, source.Ids.BattleCardId, out _);
            valid &= EnchantRoundTripTicketResolver.TryResolve(
                         firstCompleted, 1, zeroMana, deck, registry, usage, log,
                         out BattleCardInstance firstDrawn,
                         out BattleEventRecord firstDrawEvent,
                         out CardDrawFailure firstFailure) &&
                     firstFailure == CardDrawFailure.None &&
                     firstDrawn != null && firstDrawn.Zone == CardZone.Hand &&
                     firstDrawEvent.EventType == BattleEventType.CardMoved &&
                     firstDrawEvent.ParentEventId == firstCompleted.EventId &&
                     firstDrawEvent.SourceEffectId == "E03" &&
                     firstDrawEvent.FromZone == CardZone.DrawPile &&
                     firstDrawEvent.ToZone == CardZone.Hand;
            valid &= !EnchantRoundTripTicketResolver.TryResolve(
                firstCompleted, 2, zeroMana, deck, registry, usage, log, out _, out _, out _);

            BattleEventRecord sameTurnRoot = RecordRoot(log, source, "ROOT-SAME-TURN");
            valid &= BattleMainEffectEventService.TryRecordCompleted(
                log, sameTurnRoot, source.Ids.BattleCardId, out BattleEventRecord sameTurnCompleted);
            valid &= !EnchantRoundTripTicketResolver.TryResolve(
                sameTurnCompleted, 1, zeroMana, deck, registry, usage, log, out _, out _, out _);

            BattleEventRecord nextTurnRoot = RecordRoot(log, source, "ROOT-2");
            valid &= BattleMainEffectEventService.TryRecordCompleted(
                log, nextTurnRoot, source.Ids.BattleCardId, out BattleEventRecord nextTurnCompleted);
            valid &= EnchantRoundTripTicketResolver.TryResolve(
                         nextTurnCompleted, 2, zeroMana, deck, registry, usage, log,
                         out BattleCardInstance secondDrawn, out _, out CardDrawFailure secondFailure) &&
                     secondFailure == CardDrawFailure.None && secondDrawn != null &&
                     deck.DrawPileOrder.Count == 0;

            BattleManaState remainingMana = new(1);
            BattleEventRecord manaRoot = RecordRoot(log, source, "ROOT-MANA");
            valid &= BattleMainEffectEventService.TryRecordCompleted(
                log, manaRoot, source.Ids.BattleCardId, out BattleEventRecord manaCompleted);
            valid &= !EnchantRoundTripTicketResolver.TryResolve(
                manaCompleted, 3, remainingMana, deck, registry, usage, log, out _, out _, out _);
            return valid;
        }

        private static bool ValidateFullHand(
            CardData sourceCard,
            CardData fillerCard,
            EnchantData enchant)
        {
            BattleCardInstance source = CreateCard(sourceCard, "FULL-SOURCE", CardZone.Graveyard);
            BattleCardEnchantRegistry registry = CreateRegistry(source, enchant, out bool valid);
            List<BattleCardInstance> fillers = new();
            for (int i = 0; i < 11; i++)
            {
                fillers.Add(CreateCard(fillerCard, $"FULL-{i}", CardZone.DrawPile));
            }

            BattleDeckState deck = new(fillers, 2413);
            valid &= deck.DrawStartingHand() == 5;
            for (int i = 0; i < 5; i++)
            {
                valid &= deck.TryDraw(out _, out _);
            }

            valid &= deck.Zones.GetCards(CardZone.Hand).Count == 10 && deck.DrawPileOrder.Count == 1;
            string topBefore = deck.DrawPileOrder[0];
            BattleEventLog log = new();
            BattleEventRecord root = RecordRoot(log, source, "ROOT-FULL");
            valid &= BattleMainEffectEventService.TryRecordCompleted(
                log, root, source.Ids.BattleCardId, out BattleEventRecord completed);
            EnchantTurnUsageTracker usage = new();
            valid &= EnchantRoundTripTicketResolver.TryResolve(
                         completed, 1, new BattleManaState(0), deck, registry, usage, log,
                         out BattleCardInstance drawn, out BattleEventRecord drawEvent,
                         out CardDrawFailure failure) &&
                     failure == CardDrawFailure.HandFull && drawn == null && drawEvent == null &&
                     deck.DrawPileOrder.Count == 1 && deck.DrawPileOrder[0] == topBefore;
            valid &= !EnchantRoundTripTicketResolver.TryResolve(
                completed, 2, new BattleManaState(0), deck, registry, usage, log,
                out _, out _, out _);
            return valid;
        }

        private static BattleCardEnchantRegistry CreateRegistry(
            BattleCardInstance source,
            EnchantData enchant,
            out bool valid)
        {
            RunCardEnchantState state = new(source.SourceCard);
            valid = state.TryAttach(enchant, 0, false, out EnchantAttachmentFailure failure) &&
                    failure == EnchantAttachmentFailure.None;
            BattleCardEnchantRegistry registry = new();
            valid &= registry.TryRegister(source, state);
            return registry;
        }

        private static BattleEventRecord RecordRoot(
            BattleEventLog log,
            BattleCardInstance source,
            string cause)
        {
            return log.Record(
                BattleEventType.CardPlayed,
                cause,
                source.Ids.BattleCardId,
                source.Ids.BattleCardId,
                source.Ids.BattleCardId);
        }

        private static BattleCardInstance CreateCard(CardData card, string suffix, CardZone zone)
        {
            return new BattleCardInstance(
                card,
                new CardInstanceIds(
                    card.CatalogCardId,
                    $"OWNED-E03-{suffix}",
                    $"BATTLE-E03-{suffix}"),
                1,
                zone);
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
