using System;
using System.Collections.Generic;
using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class C07LostTicketValidation
    {
        [MenuItem("Have a Break/Validate C07 Lost Ticket Effect")]
        private static void ValidateFromMenu()
        {
            CardData c07 = FindCard("C07");
            CardData monsterCard = FindCard("C01");
            bool valid = c07 != null && monsterCard != null && Validate(c07, monsterCard);
            if (!valid) Debug.LogError("C07 Lost Ticket effect validation failed.");
            EditorUtility.DisplayDialog(
                "C07 Lost Ticket Effect Validation",
                valid ? "C07 Lost Ticket effect passed." :
                    "C07 Lost Ticket effect failed. Check the Console.", "OK");
        }

        private static bool Validate(CardData c07, CardData monsterCard)
        {
            BattleCardInstance source = Instance(c07, 5, "SOURCE", CardZone.Graveyard);
            List<BattleCardInstance> deckCards = new();
            for (int i = 0; i < 5; i++)
            {
                deckCards.Add(Instance(monsterCard, 1, $"DECK-{i}", CardZone.DrawPile));
            }

            BattleDeckState deck = new(deckCards, 32);
            BattleCardInstance selected = deckCards[0];
            bool valid = deck.Zones.TryMove(selected.Ids.BattleCardId, CardZone.Hand, out _);
            BattleMonsterRegistry monsters = new();
            BattleCardInstance ally = Instance(monsterCard, 1, "ALLY", CardZone.MonsterField);
            valid &= monsters.TryAdd(ally, out BattleMonsterState allyState);
            BattleEventLog log = new();
            BattleEventRecord played = log.Record(
                BattleEventType.CardPlayed, "C07Validation",
                source.Ids.BattleCardId, source.Ids.BattleCardId, selected.Ids.BattleCardId);
            valid &= C07LostTicketResolver.TryResolve(
                played, source, selected.Ids.BattleCardId, deck, monsters, log,
                new BattleEffectResolutionTracker(),
                out int drawn, out bool banished, out int defended);
            return valid && drawn == 3 && banished && defended == 1 &&
                   selected.Zone == CardZone.Banished && allyState.Defense == 2;
        }

        private static BattleCardInstance Instance(
            CardData card, int level, string suffix, CardZone zone)
        {
            return new BattleCardInstance(
                card,
                new CardInstanceIds(card.CatalogCardId, $"OWNED-C07-{suffix}", $"BATTLE-C07-{suffix}"),
                level, zone);
        }

        private static CardData FindCard(string id)
        {
            return AssetDatabase.FindAssets("t:CardData")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(path => AssetDatabase.LoadAssetAtPath<CardData>(path))
                .FirstOrDefault(card => card != null &&
                    string.Equals(card.CatalogCardId, id, StringComparison.OrdinalIgnoreCase));
        }
    }
}
